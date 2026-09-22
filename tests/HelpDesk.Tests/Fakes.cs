using System.Text.Json;
using HelpDesk.Domain.Eventos;
using HelpDesk.Infrastructure.Cache;
using HelpDesk.Infrastructure.Mensageria;

namespace HelpDesk.Tests;

/// <summary>Guarda os eventos publicados numa lista, pra o teste conferir.</summary>
public class PublicadorDeEventosFake : IPublicadorDeEventos
{
    public List<IEventoDeIntegracao> Publicados { get; } = new();

    public Task PublicarAsync(IEventoDeIntegracao evento, CancellationToken ct = default)
    {
        Publicados.Add(evento);
        return Task.CompletedTask;
    }

    public bool Publicou<T>() where T : IEventoDeIntegracao => Publicados.OfType<T>().Any();
}

/// <summary>Cache em memoria (Dictionary), sem Redis.</summary>
public class CacheEmMemoriaFake : ICacheDeLeitura
{
    private readonly Dictionary<string, string> _dados = new();

    public int Gravacoes { get; private set; }

    public Task<T?> ObterAsync<T>(string chave, CancellationToken ct = default)
    {
        if (_dados.TryGetValue(chave, out var json))
            return Task.FromResult(JsonSerializer.Deserialize<T>(json));
        return Task.FromResult<T?>(default);
    }

    public Task GravarAsync<T>(string chave, T valor, TimeSpan expiracao, CancellationToken ct = default)
    {
        _dados[chave] = JsonSerializer.Serialize(valor);
        Gravacoes++;
        return Task.CompletedTask;
    }

    public Task RemoverAsync(string chave, CancellationToken ct = default)
    {
        _dados.Remove(chave);
        return Task.CompletedTask;
    }

    public Task RemoverPorPrefixoAsync(string prefixo, CancellationToken ct = default)
    {
        foreach (var chave in _dados.Keys.Where(k => k.StartsWith(prefixo)).ToList())
            _dados.Remove(chave);
        return Task.CompletedTask;
    }
}
