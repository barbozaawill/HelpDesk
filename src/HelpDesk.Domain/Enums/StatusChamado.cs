namespace HelpDesk.Domain.Enums;

/// <summary>
/// Estados possiveis de um chamado (a "maquina de estados" da spec).
///
///   Aberto -> EmAtendimento -> AguardandoCliente -> Resolvido -> Fechado
///                          \-> Reaberto -/
///
/// As transicoes validas ficam nos metodos da entidade <see cref="Entidades.Chamado"/>.
/// </summary>
public enum StatusChamado
{
    Aberto = 0,
    EmAtendimento = 1,
    AguardandoCliente = 2,
    Resolvido = 3,
    Fechado = 4,
    Reaberto = 5
}
