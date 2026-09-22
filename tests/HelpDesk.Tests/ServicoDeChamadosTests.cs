using HelpDesk.Api.Autenticacao;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Servicos;
using HelpDesk.Domain.Common;
using HelpDesk.Domain.Entidades;
using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Eventos;
using HelpDesk.Infrastructure.Cache;
using HelpDesk.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HelpDesk.Tests;

/// <summary>
/// Testa o ServicoDeChamados com banco EF InMemory + fakes de cache e mensageria.
/// (Sem Redis nem RabbitMQ de verdade.)
/// </summary>
public class ServicoDeChamadosTests
{
    private static readonly UsuarioAtual ClienteCarlos = new(1, "Carlos", PerfilUsuario.Cliente);
    private static readonly UsuarioAtual ClienteMaria = new(2, "Maria", PerfilUsuario.Cliente);
    private static readonly UsuarioAtual AtendenteAna = new(3, "Ana", PerfilUsuario.Atendente);

    private readonly HelpDeskDbContext _db;
    private readonly PublicadorDeEventosFake _eventos = new();
    private readonly CacheEmMemoriaFake _cache = new();
    private readonly ServicoDeChamados _servico;

    public ServicoDeChamadosTests()
    {
        var options = new DbContextOptionsBuilder<HelpDeskDbContext>()
            .UseInMemoryDatabase($"chamados-{Guid.NewGuid()}")
            .Options;
        _db = new HelpDeskDbContext(options);

        _db.Categorias.Add(new Categoria { Id = 1, Nome = "Software" });
        _db.Usuarios.AddRange(
            new Usuario { Id = 1, Nome = "Carlos", Email = "carlos@x.com", Perfil = PerfilUsuario.Cliente, SenhaHash = "x" },
            new Usuario { Id = 2, Nome = "Maria", Email = "maria@x.com", Perfil = PerfilUsuario.Cliente, SenhaHash = "x" },
            new Usuario { Id = 3, Nome = "Ana", Email = "ana@x.com", Perfil = PerfilUsuario.Atendente, SenhaHash = "x" });
        _db.SaveChanges();

        var redisOpcoes = Options.Create(new RedisOpcoes { SegundosDeCachePadrao = 60 });

        _servico = new ServicoDeChamados(
            _db, _eventos, _cache, redisOpcoes, NullLogger<ServicoDeChamados>.Instance);
    }

    private static AbrirChamadoRequest RequisicaoValida() =>
        new("Erro ao salvar", "Da erro 500 ao clicar em salvar", CategoriaId: 1, Prioridade.Alta);

    [Fact]
    public async Task AbrirAsync_CriaChamado_CalculaSla_EPublicaEvento()
    {
        var dto = await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);

        Assert.Equal("Aberto", dto.Status);
        Assert.Equal("Carlos", dto.Solicitante);
        Assert.True(dto.PrazoResolucao > dto.CriadoEm);
        Assert.True(dto.PrazoPrimeiraResposta > dto.CriadoEm);

        Assert.True(_eventos.Publicou<ChamadoAbertoEvento>());
    }

    [Fact]
    public async Task AbrirAsync_ComCategoriaInexistente_Lanca()
    {
        var req = new AbrirChamadoRequest("t", "d", CategoriaId: 999, Prioridade.Baixa);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            _servico.AbrirAsync(req, ClienteCarlos, default));
    }

    [Fact]
    public async Task AssumirAsync_MudaStatus_EPublicaChamadoAssumido()
    {
        var criado = await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);

        var dto = await _servico.AssumirAsync(criado.Id, AtendenteAna, default);

        Assert.Equal("EmAtendimento", dto.Status);
        Assert.Equal("Ana", dto.Atendente);
        Assert.True(_eventos.Publicou<ChamadoAssumidoEvento>());
    }

    [Fact]
    public async Task ResolverAsync_DefineResolvidoEm_EPublicaEvento()
    {
        var criado = await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);
        await _servico.AssumirAsync(criado.Id, AtendenteAna, default);

        var dto = await _servico.ResolverAsync(criado.Id, AtendenteAna, default);

        Assert.Equal("Resolvido", dto.Status);
        Assert.NotNull(dto.ResolvidoEm);
        Assert.True(_eventos.Publicou<ChamadoResolvidoEvento>());
    }

    [Fact]
    public async Task ObterDetalheAsync_ClienteNaoVeChamadoDeOutroCliente()
    {
        var criado = await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            _servico.ObterDetalheAsync(criado.Id, ClienteMaria, default));
    }

    [Fact]
    public async Task Cliente_NaoVe_NotaInterna_NoDetalhe()
    {
        var criado = await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);
        await _servico.AssumirAsync(criado.Id, AtendenteAna, default);
        await _servico.ResponderAsync(criado.Id, new ResponderRequest("anotacao interna", Interna: true), AtendenteAna, default);

        var comoAtendente = await _servico.ObterDetalheAsync(criado.Id, AtendenteAna, default);
        var comoCliente = await _servico.ObterDetalheAsync(criado.Id, ClienteCarlos, default);

        Assert.Contains(comoAtendente.Interacoes, i => i.Interna);
        Assert.DoesNotContain(comoCliente.Interacoes, i => i.Interna);
    }

    [Fact]
    public async Task ListarAsync_UsaCache_NoSegundoAcesso()
    {
        await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);

        var primeira = await _servico.ListarAsync(new FiltroChamados(), AtendenteAna, default);

        // Insere um chamado DIRETO no banco (sem passar pelo servico -> nao invalida o cache).
        var extra = new Chamado("fora do fluxo", "inserido direto", 1, Prioridade.Baixa, 1);
        _db.Chamados.Add(extra);
        await _db.SaveChangesAsync();

        var segunda = await _servico.ListarAsync(new FiltroChamados(), AtendenteAna, default);

        Assert.Single(primeira);
        Assert.Single(segunda); // veio do cache, ainda nao "enxergou" o chamado novo
    }

    [Fact]
    public async Task Escrita_InvalidaCache_ProximaListagemVaiAoBanco()
    {
        await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);
        await _servico.ListarAsync(new FiltroChamados(), AtendenteAna, default); // popula o cache

        await _servico.AbrirAsync(RequisicaoValida(), ClienteCarlos, default);   // deve invalidar

        var lista = await _servico.ListarAsync(new FiltroChamados(), AtendenteAna, default);

        Assert.Equal(2, lista.Count);
    }
}
