using HelpDesk.Infrastructure.Cache;
using HelpDesk.Infrastructure.Persistencia;

namespace HelpDesk.Worker.Servicos;

/// <summary>
/// A cada 30s recalcula TODAS as metricas a partir do banco (fonte da verdade)
/// e grava no Redis.
///
/// Por que isso existe, se o consumidor ja mexe nos contadores a cada evento?
///  - Os eventos dao numero "na hora" (bom pra experiencia do usuario), mas sao aproximados.
///  - A reconciliacao periodica corrige qualquer desvio e recalcula o que depende de "agora"
///    (SLA estourado, resolvidos no dia...).
/// </summary>
public class ReconciliadorDeMetricas : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReconciliadorDeMetricas> _log;

    public ReconciliadorDeMetricas(IServiceScopeFactory scopeFactory, ILogger<ReconciliadorDeMetricas> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconciliarAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Falha ao reconciliar metricas. Tenta de novo no proximo ciclo.");
            }

            try
            {
                await Task.Delay(Intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReconciliarAsync(CancellationToken ct)
    {
        // BackgroundService e singleton, mas o DbContext e scoped -> abre um escopo.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
        var metricas = scope.ServiceProvider.GetRequiredService<IArmazenamentoDeMetricas>();

        var valores = await CalculadoraDeMetricas.CalcularAsync(db, ct);
        await metricas.AtualizarAsync(valores, ct);

        var estourados = (int)valores.GetValueOrDefault(Metricas.SlaEstourado);
        if (estourados > 0)
            _log.LogWarning("ATENCAO: {Qtd} chamado(s) com SLA de resolucao estourado.", estourados);

        _log.LogInformation(
            "Metricas reconciliadas -> abertos={Abertos}, em_atendimento={Atend}, sla_estourado={Sla}",
            (int)valores.GetValueOrDefault(Metricas.Abertos),
            (int)valores.GetValueOrDefault(Metricas.EmAtendimento),
            estourados);
    }
}
