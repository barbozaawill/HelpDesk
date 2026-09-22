using StackExchange.Redis;

namespace HelpDesk.Infrastructure.Cache;

/// <summary>
/// Guarda os contadores do dashboard no Redis (padrao "write-through via evento":
/// o Worker calcula e grava; a API so le o valor pronto).
///
/// Todas as metricas ficam em UM unico hash do Redis (helpdesk:metricas),
/// entao ler o dashboard inteiro e uma so ida ao Redis.
/// </summary>
public interface IArmazenamentoDeMetricas
{
    /// <summary>Soma (ou subtrai, com delta negativo) um contador. Rapido, chamado a cada evento.</summary>
    Task IncrementarAsync(string campo, long delta, CancellationToken ct = default);

    /// <summary>Sobrescreve varios campos de uma vez (usado pela reconciliacao periodica).</summary>
    Task AtualizarAsync(IReadOnlyDictionary<string, double> valores, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, double>> LerTudoAsync(CancellationToken ct = default);
}

/// <summary>Nomes dos campos do hash de metricas (pra API e Worker nao escreverem nomes diferentes).</summary>
public static class Metricas
{
    public const string Abertos = "abertos";
    public const string EmAtendimento = "em_atendimento";
    public const string AguardandoCliente = "aguardando_cliente";
    public const string ResolvidosNoDia = "resolvidos_no_dia";
    public const string FechadosNoDia = "fechados_no_dia";
    public const string SlaEstourado = "sla_estourado";
    public const string TotalAtivos = "total_ativos";
    public const string HorasMediasResolucao = "horas_medias_resolucao";
    public const string AtualizadoEmUnix = "atualizado_em_unix";
}

public sealed class ArmazenamentoDeMetricasRedis : IArmazenamentoDeMetricas
{
    public const string ChaveHash = "helpdesk:metricas";

    private readonly IConnectionMultiplexer _redis;

    public ArmazenamentoDeMetricasRedis(IConnectionMultiplexer redis) => _redis = redis;

    private IDatabase Db => _redis.GetDatabase();

    public Task IncrementarAsync(string campo, long delta, CancellationToken ct = default) =>
        Db.HashIncrementAsync(ChaveHash, campo, delta);

    public Task AtualizarAsync(IReadOnlyDictionary<string, double> valores, CancellationToken ct = default)
    {
        var entradas = valores
            .Select(par => new HashEntry(par.Key, par.Value))
            .ToArray();
        return Db.HashSetAsync(ChaveHash, entradas);
    }

    public async Task<IReadOnlyDictionary<string, double>> LerTudoAsync(CancellationToken ct = default)
    {
        var entradas = await Db.HashGetAllAsync(ChaveHash);

        var resultado = new Dictionary<string, double>();
        foreach (var e in entradas)
        {
            if (e.Value.TryParse(out double numero))
                resultado[e.Name!] = numero;
        }
        return resultado;
    }
}
