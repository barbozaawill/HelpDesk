namespace HelpDesk.Domain.Enums;

/// <summary>
/// Prioridade do chamado. E ela que define os prazos de SLA
/// (ver <see cref="Servicos.CalculadoraSla"/>).
/// </summary>
public enum Prioridade
{
    Baixa = 0,
    Media = 1,
    Alta = 2,
    Critica = 3
}
