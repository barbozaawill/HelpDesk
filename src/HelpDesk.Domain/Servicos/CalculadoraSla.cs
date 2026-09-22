using HelpDesk.Domain.Enums;

namespace HelpDesk.Domain.Servicos;

/// <summary>
/// Calcula os prazos de SLA de um chamado a partir da prioridade.
///
/// Ficou como servico separado (e nao dentro do Chamado) porque a "politica de SLA"
/// e uma decisao de negocio que muda de empresa pra empresa - e legal poder trocar
/// esses numeros sem mexer na entidade.
/// </summary>
public static class CalculadoraSla
{
    // Para cada prioridade: (prazo de primeira resposta, prazo de resolucao).
    private static readonly Dictionary<Prioridade, (TimeSpan PrimeiraResposta, TimeSpan Resolucao)> Politica = new()
    {
        [Prioridade.Baixa] = (TimeSpan.FromHours(8), TimeSpan.FromHours(72)),
        [Prioridade.Media] = (TimeSpan.FromHours(4), TimeSpan.FromHours(24)),
        [Prioridade.Alta] = (TimeSpan.FromHours(1), TimeSpan.FromHours(8)),
        [Prioridade.Critica] = (TimeSpan.FromMinutes(15), TimeSpan.FromHours(4)),
    };

    public static (DateTime PrazoPrimeiraResposta, DateTime PrazoResolucao) Calcular(
        Prioridade prioridade, DateTime aberturaUtc)
    {
        var (primeiraResposta, resolucao) = Politica[prioridade];
        return (aberturaUtc + primeiraResposta, aberturaUtc + resolucao);
    }
}
