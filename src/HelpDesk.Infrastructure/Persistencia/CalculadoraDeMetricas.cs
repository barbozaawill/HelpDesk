using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Persistencia;

/// <summary>
/// Calcula TODAS as metricas do dashboard direto do banco (fonte da verdade).
///
/// Usada em dois lugares:
///  - Worker: um job periodico chama isso e grava o resultado no Redis (reconciliacao).
///  - API: se o Redis estiver vazio (Worker ainda nao rodou), usa isso como fallback.
/// </summary>
public static class CalculadoraDeMetricas
{
    public static async Task<IReadOnlyDictionary<string, double>> CalcularAsync(
        HelpDeskDbContext db, CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var inicioDoDia = new DateTime(agora.Year, agora.Month, agora.Day, 0, 0, 0, DateTimeKind.Utc);

        var porStatus = await db.Chamados
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Qtd = g.Count() })
            .ToListAsync(ct);

        int Contar(StatusChamado status) => porStatus.FirstOrDefault(x => x.Status == status)?.Qtd ?? 0;

        var abertos = Contar(StatusChamado.Aberto) + Contar(StatusChamado.Reaberto);
        var emAtendimento = Contar(StatusChamado.EmAtendimento);
        var aguardandoCliente = Contar(StatusChamado.AguardandoCliente);

        var slaEstourado = await db.Chamados.CountAsync(c =>
            c.Status != StatusChamado.Resolvido &&
            c.Status != StatusChamado.Fechado &&
            c.PrazoResolucao < agora, ct);

        var resolvidosNoDia = await db.Chamados.CountAsync(c => c.ResolvidoEm != null && c.ResolvidoEm >= inicioDoDia, ct);
        var fechadosNoDia = await db.Chamados.CountAsync(c => c.FechadoEm != null && c.FechadoEm >= inicioDoDia, ct);

        // Tempo medio de resolucao dos ultimos 30 dias (em horas).
        var limite = agora.AddDays(-30);
        var duracoes = await db.Chamados
            .Where(c => c.ResolvidoEm != null && c.ResolvidoEm >= limite)
            .Select(c => new { Inicio = c.CriadoEm, Fim = c.ResolvidoEm!.Value })
            .ToListAsync(ct);

        var horasMedias = duracoes.Count == 0
            ? 0
            : duracoes.Average(d => (d.Fim - d.Inicio).TotalHours);

        return new Dictionary<string, double>
        {
            [Metricas.Abertos] = abertos,
            [Metricas.EmAtendimento] = emAtendimento,
            [Metricas.AguardandoCliente] = aguardandoCliente,
            [Metricas.ResolvidosNoDia] = resolvidosNoDia,
            [Metricas.FechadosNoDia] = fechadosNoDia,
            [Metricas.SlaEstourado] = slaEstourado,
            [Metricas.TotalAtivos] = abertos + emAtendimento + aguardandoCliente,
            [Metricas.HorasMediasResolucao] = Math.Round(horasMedias, 1),
            [Metricas.AtualizadoEmUnix] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
    }
}
