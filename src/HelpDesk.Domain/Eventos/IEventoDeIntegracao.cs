namespace HelpDesk.Domain.Eventos;

/// <summary>
/// Contrato dos eventos publicados no RabbitMQ quando o chamado muda de estado.
///
/// Fica no Dominio de proposito: tanto a API (que PUBLICA) quanto o Worker (que CONSOME)
/// precisam enxergar a mesma classe pra serializar/desserializar o JSON do mesmo jeito.
/// </summary>
public interface IEventoDeIntegracao
{
    /// <summary>
    /// Routing key usada na exchange do tipo "topic" (ex: "chamado.aberto").
    /// E por ela que o RabbitMQ decide pra quais filas a mensagem vai.
    /// </summary>
    string RoutingKey { get; }

    DateTime OcorridoEm { get; }
}
