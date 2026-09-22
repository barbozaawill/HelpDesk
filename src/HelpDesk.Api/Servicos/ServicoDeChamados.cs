using HelpDesk.Api.Autenticacao;
using HelpDesk.Api.Dtos;
using HelpDesk.Domain.Common;
using HelpDesk.Domain.Entidades;
using HelpDesk.Domain.Eventos;
using HelpDesk.Domain.Servicos;
using HelpDesk.Infrastructure.Cache;
using HelpDesk.Infrastructure.Mensageria;
using HelpDesk.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Servicos;

/// <summary>
/// Orquestra o caso de uso "chamado": carrega do banco, chama o metodo da entidade
/// (que valida a maquina de estados), salva, invalida o cache e publica o evento.
///
/// A regra de negocio "de verdade" mora na entidade <see cref="Chamado"/>;
/// aqui e so a cola entre banco + cache + mensageria.
/// </summary>
public class ServicoDeChamados
{
    private const string PrefixoCacheListas = "chamados:lista:";

    private readonly HelpDeskDbContext _db;
    private readonly IPublicadorDeEventos _eventos;
    private readonly ICacheDeLeitura _cache;
    private readonly RedisOpcoes _redisOpcoes;
    private readonly ILogger<ServicoDeChamados> _log;

    public ServicoDeChamados(
        HelpDeskDbContext db,
        IPublicadorDeEventos eventos,
        ICacheDeLeitura cache,
        IOptions<RedisOpcoes> redisOpcoes,
        ILogger<ServicoDeChamados> log)
    {
        _db = db;
        _eventos = eventos;
        _cache = cache;
        _redisOpcoes = redisOpcoes.Value;
        _log = log;
    }

    // ------------------------------------------------------------------ Leitura

    /// <summary>Listagem com cache-aside: tenta o Redis primeiro, so vai no banco se der "miss".</summary>
    public async Task<IReadOnlyList<ChamadoResumoDto>> ListarAsync(
        FiltroChamados filtro, UsuarioAtual usuario, CancellationToken ct)
    {
        // Cliente so enxerga os proprios chamados; atendente/admin enxergam todos.
        // Por isso o "escopo" entra na chave do cache (senao um cliente veria a lista do outro).
        var escopo = usuario.EhAtendente ? "equipe" : $"cli:{usuario.Id}";
        var statusChave = filtro.Status?.ToString() ?? "todos";
        var chaveCache = $"{PrefixoCacheListas}{escopo}:{statusChave}";

        var emCache = await _cache.ObterAsync<List<ChamadoResumoDto>>(chaveCache, ct);
        if (emCache is not null)
        {
            _log.LogInformation("Listagem servida do CACHE ({Chave})", chaveCache);
            return emCache;
        }

        _log.LogInformation("Cache MISS ({Chave}) - consultando o banco", chaveCache);

        IQueryable<Chamado> query = _db.Chamados
            .AsNoTracking()
            .Include(c => c.Categoria)
            .Include(c => c.Solicitante)
            .Include(c => c.Atendente);

        if (!usuario.EhAtendente)
            query = query.Where(c => c.SolicitanteId == usuario.Id);

        if (filtro.Status is not null)
            query = query.Where(c => c.Status == filtro.Status.Value);

        var chamados = await query
            .OrderByDescending(c => c.CriadoEm)
            .Take(200)
            .ToListAsync(ct);

        var resultado = chamados.Select(ParaResumo).ToList();

        var ttl = TimeSpan.FromSeconds(Math.Max(5, _redisOpcoes.SegundosDeCachePadrao));
        await _cache.GravarAsync(chaveCache, resultado, ttl, ct);

        return resultado;
    }

    public async Task<ChamadoDetalheDto> ObterDetalheAsync(int id, UsuarioAtual usuario, CancellationToken ct)
    {
        var chamado = await CarregarAsync(id, ct);
        GarantirAcesso(chamado, usuario);
        return ParaDetalhe(chamado, usuario);
    }

    // ------------------------------------------------------------------ Escrita

    public async Task<ChamadoDetalheDto> AbrirAsync(AbrirChamadoRequest req, UsuarioAtual usuario, CancellationToken ct)
    {
        var categoriaValida = await _db.Categorias.AnyAsync(c => c.Id == req.CategoriaId && c.Ativa, ct);
        if (!categoriaValida)
            throw new RegraDeNegocioException("Categoria invalida ou inativa.");

        // O construtor da entidade valida titulo/descricao.
        var chamado = new Chamado(req.Titulo, req.Descricao, req.CategoriaId, req.Prioridade, usuario.Id);

        var (prazoResposta, prazoResolucao) = CalculadoraSla.Calcular(chamado.Prioridade, chamado.CriadoEm);
        chamado.DefinirPrazosSla(prazoResposta, prazoResolucao);

        _db.Chamados.Add(chamado);
        await _db.SaveChangesAsync(ct);

        await InvalidarCacheDeListas(ct);
        await PublicarAsync(new ChamadoAbertoEvento(
            chamado.Id, chamado.Titulo, chamado.Prioridade, usuario.Id, chamado.PrazoResolucao, DateTime.UtcNow), ct);

        var completo = await CarregarAsync(chamado.Id, ct);
        return ParaDetalhe(completo, usuario);
    }

    public async Task<ChamadoDetalheDto> AssumirAsync(int id, UsuarioAtual usuario, CancellationToken ct)
    {
        var chamado = await CarregarAsync(id, ct);

        chamado.Assumir(usuario.Id);
        await _db.SaveChangesAsync(ct);

        await InvalidarCacheDeListas(ct);
        await PublicarAsync(new ChamadoAssumidoEvento(chamado.Id, usuario.Id, DateTime.UtcNow), ct);

        return ParaDetalhe(await CarregarAsync(id, ct), usuario);
    }

    public async Task<ChamadoDetalheDto> ResponderAsync(
        int id, ResponderRequest req, UsuarioAtual usuario, CancellationToken ct)
    {
        var chamado = await CarregarAsync(id, ct);
        GarantirAcesso(chamado, usuario);

        // So atendente pode escrever "nota interna".
        var interna = req.Interna && usuario.EhAtendente;
        chamado.AdicionarInteracao(usuario.Id, req.Mensagem, usuario.EhAtendente, interna);

        await _db.SaveChangesAsync(ct);

        await InvalidarCacheDeListas(ct);
        await PublicarAsync(new ChamadoAtualizadoEvento(chamado.Id, usuario.Id, usuario.EhAtendente, DateTime.UtcNow), ct);

        return ParaDetalhe(await CarregarAsync(id, ct), usuario);
    }

    public async Task<ChamadoDetalheDto> ResolverAsync(int id, UsuarioAtual usuario, CancellationToken ct)
    {
        var chamado = await CarregarAsync(id, ct);

        chamado.Resolver();
        await _db.SaveChangesAsync(ct);

        await InvalidarCacheDeListas(ct);
        await PublicarAsync(new ChamadoResolvidoEvento(
            chamado.Id, chamado.AtendenteId, chamado.TempoDeResolucao?.TotalHours ?? 0, DateTime.UtcNow), ct);

        return ParaDetalhe(await CarregarAsync(id, ct), usuario);
    }

    public async Task<ChamadoDetalheDto> FecharAsync(int id, UsuarioAtual usuario, CancellationToken ct)
    {
        var chamado = await CarregarAsync(id, ct);
        GarantirAcesso(chamado, usuario);

        chamado.Fechar();
        await _db.SaveChangesAsync(ct);

        await InvalidarCacheDeListas(ct);
        await PublicarAsync(new ChamadoFechadoEvento(chamado.Id, DateTime.UtcNow), ct);

        return ParaDetalhe(await CarregarAsync(id, ct), usuario);
    }

    public async Task<ChamadoDetalheDto> ReabrirAsync(int id, UsuarioAtual usuario, CancellationToken ct)
    {
        var chamado = await CarregarAsync(id, ct);
        GarantirAcesso(chamado, usuario);

        chamado.Reabrir();
        await _db.SaveChangesAsync(ct);

        await InvalidarCacheDeListas(ct);
        await PublicarAsync(new ChamadoReabertoEvento(chamado.Id, chamado.SolicitanteId, DateTime.UtcNow), ct);

        return ParaDetalhe(await CarregarAsync(id, ct), usuario);
    }

    // ------------------------------------------------------------------ Helpers

    private async Task<Chamado> CarregarAsync(int id, CancellationToken ct)
    {
        var chamado = await _db.Chamados
            .Include(c => c.Categoria)
            .Include(c => c.Solicitante)
            .Include(c => c.Atendente)
            .Include(c => c.Interacoes).ThenInclude(i => i.Autor)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return chamado ?? throw RecursoNaoEncontradoException.Chamado(id);
    }

    private static void GarantirAcesso(Chamado chamado, UsuarioAtual usuario)
    {
        if (!usuario.EhAtendente && chamado.SolicitanteId != usuario.Id)
            // 404 de proposito (e nao 403): nao confirmamos nem negamos que o chamado existe.
            throw RecursoNaoEncontradoException.Chamado(chamado.Id);
    }

    private async Task InvalidarCacheDeListas(CancellationToken ct)
    {
        try
        {
            await _cache.RemoverPorPrefixoAsync(PrefixoCacheListas, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Nao consegui limpar o cache de listagens (Redis fora do ar?).");
        }
    }

    private async Task PublicarAsync(IEventoDeIntegracao evento, CancellationToken ct)
    {
        try
        {
            await _eventos.PublicarAsync(evento, ct);
        }
        catch (Exception ex)
        {
            // O chamado JA foi salvo no banco. Se o RabbitMQ estiver fora, logamos e seguimos:
            // e melhor perder a notificacao do que estourar a request do usuario.
            // (Num sistema real isso se resolve com o padrao "Outbox" - ver README.)
            _log.LogError(ex, "Falha ao publicar {Evento}. O chamado foi salvo mesmo assim.",
                evento.GetType().Name);
        }
    }

    private static ChamadoResumoDto ParaResumo(Chamado c) => new(
        c.Id,
        c.Titulo,
        c.Status.ToString(),
        c.Prioridade.ToString(),
        c.Categoria?.Nome ?? "-",
        c.Solicitante?.Nome ?? "-",
        c.Atendente?.Nome,
        c.CriadoEm,
        c.PrazoResolucao,
        c.SlaResolucaoEstourado);

    private static ChamadoDetalheDto ParaDetalhe(Chamado c, UsuarioAtual usuario)
    {
        var interacoes = c.Interacoes
            .Where(i => usuario.EhAtendente || !i.Interna) // cliente nao ve nota interna
            .OrderBy(i => i.CriadaEm)
            .Select(i => new InteracaoDto(
                i.Id, i.Autor?.Nome ?? "-", i.AutorId, i.Mensagem, i.Interna, i.CriadaEm))
            .ToList();

        return new ChamadoDetalheDto(
            c.Id, c.Titulo, c.Descricao, c.Status.ToString(), c.Prioridade.ToString(),
            c.CategoriaId, c.Categoria?.Nome ?? "-",
            c.SolicitanteId, c.Solicitante?.Nome ?? "-",
            c.AtendenteId, c.Atendente?.Nome,
            c.CriadoEm, c.AtualizadoEm, c.ResolvidoEm, c.FechadoEm,
            c.PrazoPrimeiraResposta, c.PrazoResolucao, c.SlaResolucaoEstourado,
            interacoes);
    }
}
