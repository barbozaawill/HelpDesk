using HelpDesk.Domain.Entidades;
using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Servicos;
using HelpDesk.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Persistencia;

/// <summary>
/// Aplica as migrations pendentes e popula dados iniciais (categorias + usuarios de teste
/// + 1 chamado de exemplo). Chamado no boot da API.
///
/// E "idempotente": pode rodar toda vez que a API sobe que so cria o que ainda nao existe.
/// (Rodar migration no startup e pratico pra estudo/demo; num sistema grande isso viraria
///  um passo separado do deploy.)
/// </summary>
public static class InicializadorDoBanco
{
    public static async Task PrepararAsync(HelpDeskDbContext ctx, IHashDeSenha hash, CancellationToken ct = default)
    {
        await MigrarComRetryAsync(ctx, ct);

        await SemearCategorias(ctx, ct);
        await SemearUsuarios(ctx, hash, ct);
        await SemearChamadoDeExemplo(ctx, ct);
    }

    /// <summary>
    /// No docker-compose o Postgres pode levar alguns segundos pra aceitar conexao.
    /// Tenta aplicar as migrations algumas vezes antes de desistir.
    /// </summary>
    private static async Task MigrarComRetryAsync(HelpDeskDbContext ctx, CancellationToken ct)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await ctx.Database.MigrateAsync(ct);
                return;
            }
            catch (Exception) when (tentativa < 10 && !ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    private static async Task SemearCategorias(HelpDeskDbContext ctx, CancellationToken ct)
    {
        if (await ctx.Categorias.AnyAsync(ct))
            return;

        ctx.Categorias.AddRange(
            new Categoria { Nome = "Hardware" },
            new Categoria { Nome = "Software" },
            new Categoria { Nome = "Rede" },
            new Categoria { Nome = "Acesso" });

        await ctx.SaveChangesAsync(ct);
    }

    private static async Task SemearUsuarios(HelpDeskDbContext ctx, IHashDeSenha hash, CancellationToken ct)
    {
        if (await ctx.Usuarios.AnyAsync(ct))
            return;

        ctx.Usuarios.AddRange(
            new Usuario
            {
                Nome = "Admin", Email = "admin@helpdesk.local",
                Perfil = PerfilUsuario.Admin, SenhaHash = hash.Gerar("Admin@123")
            },
            new Usuario
            {
                Nome = "Ana Atendente", Email = "ana@helpdesk.local",
                Perfil = PerfilUsuario.Atendente, SenhaHash = hash.Gerar("Atende@123")
            },
            new Usuario
            {
                Nome = "Carlos Cliente", Email = "carlos@cliente.local",
                Perfil = PerfilUsuario.Cliente, SenhaHash = hash.Gerar("Cliente@123")
            });

        await ctx.SaveChangesAsync(ct);
    }

    private static async Task SemearChamadoDeExemplo(HelpDeskDbContext ctx, CancellationToken ct)
    {
        if (await ctx.Chamados.AnyAsync(ct))
            return;

        var cliente = await ctx.Usuarios
            .OrderBy(u => u.Id)
            .FirstAsync(u => u.Perfil == PerfilUsuario.Cliente, ct);
        var categoria = await ctx.Categorias.OrderBy(c => c.Id).FirstAsync(ct);

        var chamado = new Chamado(
            "Nao consigo acessar o e-mail",
            "Desde hoje de manha aparece 'senha invalida' ao entrar no webmail.",
            categoria.Id, Prioridade.Alta, cliente.Id);

        var (prazoResposta, prazoResolucao) = CalculadoraSla.Calcular(chamado.Prioridade, chamado.CriadoEm);
        chamado.DefinirPrazosSla(prazoResposta, prazoResolucao);

        ctx.Chamados.Add(chamado);
        await ctx.SaveChangesAsync(ct);
    }
}
