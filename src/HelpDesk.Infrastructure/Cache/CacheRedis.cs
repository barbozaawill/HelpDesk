using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HelpDesk.Infrastructure.Cache;

/// <summary>
/// Cache de leitura (padrao "cache-aside"): a API pergunta aqui antes de ir no banco.
/// </summary>
public interface ICacheDeLeitura
{
    Task<T?> ObterAsync<T>(string chave, CancellationToken ct = default);
    Task GravarAsync<T>(string chave, T valor, TimeSpan expiracao, CancellationToken ct = default);
    Task RemoverAsync(string chave, CancellationToken ct = default);

    /// <summary>Apaga todas as chaves que comecam com o prefixo (ex: "chamados:lista:").</summary>
    Task RemoverPorPrefixoAsync(string prefixo, CancellationToken ct = default);
}

public sealed class CacheRedis : ICacheDeLeitura
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheRedis> _log;

    public CacheRedis(IConnectionMultiplexer redis, ILogger<CacheRedis> log)
    {
        _redis = redis;
        _log = log;
    }

    private IDatabase Db => _redis.GetDatabase();

    public async Task<T?> ObterAsync<T>(string chave, CancellationToken ct = default)
    {
        var valor = await Db.StringGetAsync(chave);
        if (valor.IsNullOrEmpty)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(valor!, JsonOpts);
        }
        catch (JsonException ex)
        {
            // Se o formato guardado mudou, e melhor tratar como "cache miss" do que estourar erro.
            _log.LogWarning(ex, "Nao consegui ler o cache da chave {Chave}; tratando como miss.", chave);
            return default;
        }
    }

    public Task GravarAsync<T>(string chave, T valor, TimeSpan expiracao, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(valor, JsonOpts);
        return Db.StringSetAsync(chave, json, expiracao);
    }

    public Task RemoverAsync(string chave, CancellationToken ct = default) =>
        Db.KeyDeleteAsync(chave);

    public async Task RemoverPorPrefixoAsync(string prefixo, CancellationToken ct = default)
    {
        // Percorre as chaves com SCAN (nao trava o Redis como o KEYS faria).
        // Funciona bem pra um Redis single-node de dev; em cluster a estrategia mudaria.
        foreach (var endpoint in _redis.GetEndPoints())
        {
            var server = _redis.GetServer(endpoint);
            if (!server.IsConnected || server.IsReplica)
                continue;

            await foreach (var chave in server.KeysAsync(pattern: prefixo + "*").WithCancellation(ct))
                await Db.KeyDeleteAsync(chave);
        }
    }
}
