using HelpDesk.Domain.Enums;

namespace HelpDesk.Domain.Eventos;

// Um "record" por transicao relevante da maquina de estados.
// A routing key segue o padrao "chamado.<verbo>" pra combinar com a exchange topic.

public record ChamadoAbertoEvento(
    int ChamadoId,
    string Titulo,
    Prioridade Prioridade,
    int SolicitanteId,
    DateTime PrazoResolucao,
    DateTime OcorridoEm) : IEventoDeIntegracao
{
    public string RoutingKey => "chamado.aberto";
}

public record ChamadoAssumidoEvento(
    int ChamadoId,
    int AtendenteId,
    DateTime OcorridoEm) : IEventoDeIntegracao
{
    public string RoutingKey => "chamado.assumido";
}

public record ChamadoAtualizadoEvento(
    int ChamadoId,
    int AutorId,
    bool AutorEhAtendente,
    DateTime OcorridoEm) : IEventoDeIntegracao
{
    public string RoutingKey => "chamado.atualizado";
}

public record ChamadoResolvidoEvento(
    int ChamadoId,
    int? AtendenteId,
    double HorasAteResolver,
    DateTime OcorridoEm) : IEventoDeIntegracao
{
    public string RoutingKey => "chamado.resolvido";
}

public record ChamadoFechadoEvento(
    int ChamadoId,
    DateTime OcorridoEm) : IEventoDeIntegracao
{
    public string RoutingKey => "chamado.fechado";
}

public record ChamadoReabertoEvento(
    int ChamadoId,
    int SolicitanteId,
    DateTime OcorridoEm) : IEventoDeIntegracao
{
    public string RoutingKey => "chamado.reaberto";
}
