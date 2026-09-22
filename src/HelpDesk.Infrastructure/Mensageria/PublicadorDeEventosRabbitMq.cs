using System.Text.Json;
using HelpDesk.Domain.Eventos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HelpDesk.Infrastructure.Mensageria;

/// <summary>
/// Publica eventos de dominio no RabbitMQ. A API usa isso depois de salvar
/// uma mudanca no banco (ex: chamado aberto -> publica ChamadoAbertoEvento).
/// </summary>
public interface IPublicadorDeEventos
{
    Task PublicarAsync(IEventoDeIntegracao evento, CancellationToken ct = default);
}

public sealed class PublicadorDeEventosRabbitMq : IPublicadorDeEventos, IDisposable
{
    // Mesmas opcoes de JSON que o Worker usa pra ler (camelCase, case-insensitive).
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOpcoes _opcoes;
    private readonly ILogger<PublicadorDeEventosRabbitMq> _log;

    // A conexao e cara de abrir, entao abrimos uma vez (na primeira publicacao) e reaproveitamos.
    private readonly Lazy<IConnection> _conexao;

    public PublicadorDeEventosRabbitMq(
        IOptions<RabbitMqOpcoes> opcoes,
        ILogger<PublicadorDeEventosRabbitMq> log)
    {
        _opcoes = opcoes.Value;
        _log = log;
        _conexao = new Lazy<IConnection>(CriarConexaoComRetry);
    }

    public Task PublicarAsync(IEventoDeIntegracao evento, CancellationToken ct = default)
    {
        // O canal (IModel) NAO e thread-safe, entao criamos um por publicacao.
        using var canal = _conexao.Value.CreateModel();

        canal.ExchangeDeclare(MensageriaConstantes.ExchangeChamados, ExchangeType.Topic, durable: true);

        // Serializa usando o tipo concreto (senao so ia o que esta na interface).
        var corpo = JsonSerializer.SerializeToUtf8Bytes(evento, evento.GetType(), JsonOpts);

        var props = canal.CreateBasicProperties();
        props.Persistent = true;                 // mensagem sobrevive a um restart do RabbitMQ
        props.ContentType = "application/json";
        props.Type = evento.GetType().Name;      // dica pro consumidor
        props.MessageId = Guid.NewGuid().ToString();

        canal.BasicPublish(
            exchange: MensageriaConstantes.ExchangeChamados,
            routingKey: evento.RoutingKey,
            basicProperties: props,
            body: corpo);

        _log.LogInformation("Evento publicado: {Evento} (routing key '{RoutingKey}')",
            evento.GetType().Name, evento.RoutingKey);

        return Task.CompletedTask;
    }

    private IConnection CriarConexaoComRetry()
    {
        var factory = new ConnectionFactory
        {
            HostName = _opcoes.Host,
            Port = _opcoes.Porta,
            UserName = _opcoes.Usuario,
            Password = _opcoes.Senha,
            VirtualHost = _opcoes.VirtualHost
        };

        // No docker-compose o RabbitMQ pode levar alguns segundos pra aceitar conexao.
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                return factory.CreateConnection("helpdesk-publisher");
            }
            catch (Exception ex) when (tentativa < 10)
            {
                _log.LogWarning(ex,
                    "RabbitMQ indisponivel (tentativa {Tentativa}/10). Tentando de novo em 3s...", tentativa);
                Thread.Sleep(3000);
            }
        }
    }

    public void Dispose()
    {
        if (_conexao.IsValueCreated)
            _conexao.Value.Dispose();
    }
}
