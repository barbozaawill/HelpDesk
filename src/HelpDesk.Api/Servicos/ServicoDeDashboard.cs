using HelpDesk.Api.Dtos;
using HelpDesk.Infrastructure.Cache;
using HelpDesk.Infrastructure.Persistencia;

namespace HelpDesk.Api.Servicos;

/// <summary>
/// Monta o DTO do dashboard. O caminho feliz e so LER o Redis (valores que o Worker
/// deixou prontos). Se o Redis estiver vazio, cai pro fallback: calcula na hora do banco.
/// </summary>
public class ServicoDeDashboard
{
    private readonly IArmazenamentoDeMetricas _metricas;
    private readonly HelpDeskDbContext _db;
    private readonly ILogger<ServicoDeDashboard> _log;

    public ServicoDeDashboard(
        IArmazenamentoDeMetricas metricas, HelpDeskDbContext db, ILogger<ServicoDeDashboard> log)
    {
        _metricas = metricas;
        _db = db;
        _log = log;
    }

    public async Task<MetricasDto> ObterAsync(CancellationToken ct)
    {
        var valores = await _metricas.LerTudoAsync(ct);

        if (valores.Count > 0)
            return Montar(valores, origem: "redis (mantido pelo Worker)");

        _log.LogWarning("Redis sem metricas - calculando do banco (o Worker ja subiu?).");
        var doBanco = await CalculadoraDeMetricas.CalcularAsync(_db, ct);
        return Montar(doBanco, origem: "banco (fallback)");
    }

    private static MetricasDto Montar(IReadOnlyDictionary<string, double> v, string origem)
    {
        int Pegar(string campo) => v.TryGetValue(campo, out var n) ? (int)Math.Round(n) : 0;

        DateTime? atualizadoEm = v.TryGetValue(Metricas.AtualizadoEmUnix, out var unix) && unix > 0
            ? DateTimeOffset.FromUnixTimeSeconds((long)unix).UtcDateTime
            : null;

        return new MetricasDto(
            Abertos: Pegar(Metricas.Abertos),
            EmAtendimento: Pegar(Metricas.EmAtendimento),
            AguardandoCliente: Pegar(Metricas.AguardandoCliente),
            ResolvidosNoDia: Pegar(Metricas.ResolvidosNoDia),
            FechadosNoDia: Pegar(Metricas.FechadosNoDia),
            SlaEstourado: Pegar(Metricas.SlaEstourado),
            TotalAtivos: Pegar(Metricas.TotalAtivos),
            HorasMediasResolucao: v.TryGetValue(Metricas.HorasMediasResolucao, out var h) ? Math.Round(h, 1) : 0,
            AtualizadoEm: atualizadoEm,
            Origem: origem);
    }
}
