using System.Text.Json;
using HelpDesk.Domain.Eventos;
using HelpDesk.Infrastructure.Cache;

namespace HelpDesk.Worker.Processadores;

/// <summary>
/// Recebe um evento (ja como JSON + routing key) e reage a ele:
///  - atualiza os contadores rapidos no Redis (HINCRBY);
///  - "envia" a notificacao (que aqui e so um log, como a spec pede).
///
/// Os contadores daqui sao APROXIMADOS de proposito (a ordem dos eventos pode variar).
/// Quem garante o numero exato e o <see cref="Servicos.ReconciliadorDeMetricas"/>,
/// que recalcula tudo do banco a cada 30s.
/// </summary>
public class ProcessadorDeEventos
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly IArmazenamentoDeMetricas _metricas;
    private readonly ILogger<ProcessadorDeEventos> _log;

    public ProcessadorDeEventos(IArmazenamentoDeMetricas metricas, ILogger<ProcessadorDeEventos> log)
    {
        _metricas = metricas;
        _log = log;
    }

    public async Task ProcessarAsync(string routingKey, string json, CancellationToken ct)
    {
        switch (routingKey)
        {
            case "chamado.aberto":
            {
                var e = Ler<ChamadoAbertoEvento>(json);
                await _metricas.IncrementarAsync(Metricas.Abertos, +1, ct);
                await _metricas.IncrementarAsync(Metricas.TotalAtivos, +1, ct);
                Notificar($"Novo chamado #{e.ChamadoId} \"{e.Titulo}\" (prioridade {e.Prioridade}). " +
                          $"Prazo de resolucao: {e.PrazoResolucao.ToLocalTime():dd/MM HH:mm}.");
                break;
            }
            case "chamado.assumido":
            {
                var e = Ler<ChamadoAssumidoEvento>(json);
                await _metricas.IncrementarAsync(Metricas.Abertos, -1, ct);
                await _metricas.IncrementarAsync(Metricas.EmAtendimento, +1, ct);
                Notificar($"Chamado #{e.ChamadoId} assumido pelo atendente #{e.AtendenteId}.");
                break;
            }
            case "chamado.atualizado":
            {
                var e = Ler<ChamadoAtualizadoEvento>(json);
                var quem = e.AutorEhAtendente ? "atendente" : "cliente";
                Notificar($"Nova resposta ({quem}) no chamado #{e.ChamadoId}.");
                break;
            }
            case "chamado.resolvido":
            {
                var e = Ler<ChamadoResolvidoEvento>(json);
                await _metricas.IncrementarAsync(Metricas.EmAtendimento, -1, ct);
                await _metricas.IncrementarAsync(Metricas.TotalAtivos, -1, ct);
                await _metricas.IncrementarAsync(Metricas.ResolvidosNoDia, +1, ct);
                Notificar($"Chamado #{e.ChamadoId} resolvido em {e.HorasAteResolver:0.0}h.");
                break;
            }
            case "chamado.fechado":
            {
                var e = Ler<ChamadoFechadoEvento>(json);
                await _metricas.IncrementarAsync(Metricas.FechadosNoDia, +1, ct);
                Notificar($"Chamado #{e.ChamadoId} fechado.");
                break;
            }
            case "chamado.reaberto":
            {
                var e = Ler<ChamadoReabertoEvento>(json);
                await _metricas.IncrementarAsync(Metricas.Abertos, +1, ct);
                await _metricas.IncrementarAsync(Metricas.TotalAtivos, +1, ct);
                Notificar($"Chamado #{e.ChamadoId} foi REABERTO pelo cliente #{e.SolicitanteId}.");
                break;
            }
            default:
                _log.LogWarning("Routing key sem tratamento: {RoutingKey}", routingKey);
                break;
        }
    }

    private static T Ler<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOpts)
        ?? throw new InvalidOperationException($"JSON invalido para o evento {typeof(T).Name}.");

    private void Notificar(string mensagem) =>
        _log.LogInformation("NOTIFICACAO >> {Mensagem}", mensagem);
}
