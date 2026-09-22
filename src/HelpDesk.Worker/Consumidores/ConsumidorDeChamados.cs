using System.Text;
using HelpDesk.Infrastructure.Mensageria;
using HelpDesk.Worker.Processadores;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HelpDesk.Worker.Consumidores;

/// <summary>
/// Servico que fica ligado no RabbitMQ consumindo a fila do Worker.
/// Para cada mensagem chama o <see cref="ProcessadorDeEventos"/> e so confirma (ack)
/// se o processamento deu certo.
/// </summary>
public class ConsumidorDeChamados : BackgroundService
{
    private readonly RabbitMqOpcoes _opcoes;
    private readonly ProcessadorDeEventos _processador;
    private readonly ILogger<ConsumidorDeChamados> _log;

    private IConnection? _conexao;
    private IModel? _canal;

    public ConsumidorDeChamados(
        IOptions<RabbitMqOpcoes> opcoes,
        ProcessadorDeEventos processador,
        ILogger<ConsumidorDeChamados> log)
    {
        _opcoes = opcoes.Value;
        _processador = processador;
        _log = log;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AbrirConexaoEDeclararFila(stoppingToken);

        var consumidor = new AsyncEventingBasicConsumer(_canal!);
        consumidor.Received += AoReceberMensagem;

        _canal!.BasicConsume(
            queue: MensageriaConstantes.FilaWorker,
            autoAck: false,
            consumer: consumidor);

        _log.LogInformation("Consumindo a fila '{Fila}' (exchange topic '{Exchange}').",
            MensageriaConstantes.FilaWorker, MensageriaConstantes.ExchangeChamados);

        // O consumo roda em background por conta do RabbitMQ; aqui so encerramos o metodo.
        return Task.CompletedTask;
    }

    private void AbrirConexaoEDeclararFila(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opcoes.Host,
            Port = _opcoes.Porta,
            UserName = _opcoes.Usuario,
            Password = _opcoes.Senha,
            VirtualHost = _opcoes.VirtualHost,
            DispatchConsumersAsync = true
        };

        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                _conexao = factory.CreateConnection("helpdesk-worker");
                break;
            }
            catch (Exception ex) when (tentativa < 15 && !ct.IsCancellationRequested)
            {
                _log.LogWarning(ex, "RabbitMQ indisponivel (tentativa {N}/15). Nova tentativa em 3s...", tentativa);
                Thread.Sleep(3000);
            }
        }

        _canal = _conexao!.CreateModel();

        // Declara exchange + fila + binding. Se ja existirem, o RabbitMQ apenas confirma.
        _canal.ExchangeDeclare(MensageriaConstantes.ExchangeChamados, ExchangeType.Topic, durable: true);
        _canal.QueueDeclare(MensageriaConstantes.FilaWorker,
            durable: true, exclusive: false, autoDelete: false);
        _canal.QueueBind(MensageriaConstantes.FilaWorker, MensageriaConstantes.ExchangeChamados,
            MensageriaConstantes.RoutingKeyTodosOsChamados);

        // No maximo 10 mensagens "sem ack" por vez.
        _canal.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
    }

    private async Task AoReceberMensagem(object sender, BasicDeliverEventArgs ea)
    {
        var json = Encoding.UTF8.GetString(ea.Body.Span);
        try
        {
            await _processador.ProcessarAsync(ea.RoutingKey, json, CancellationToken.None);
            _canal!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Erro ao processar mensagem (routing key '{Rk}'). Descartando.", ea.RoutingKey);
            // requeue:false para nao entrar em loop infinito.
            // Num sistema real, isso iria para uma Dead Letter Queue.
            _canal!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        try { _canal?.Close(); } catch { /* ok */ }
        try { _conexao?.Close(); } catch { /* ok */ }
        _canal?.Dispose();
        _conexao?.Dispose();
        base.Dispose();
    }
}
