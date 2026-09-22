namespace HelpDesk.Infrastructure.Mensageria;

/// <summary>Configuracao de conexao com o RabbitMQ (lida do appsettings, secao "RabbitMq").</summary>
public class RabbitMqOpcoes
{
    public const string Secao = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Porta { get; set; } = 5672;
    public string Usuario { get; set; } = "guest";
    public string Senha { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}

/// <summary>Nomes fixos de exchange/fila/routing key usados pela mensageria.</summary>
public static class MensageriaConstantes
{
    /// <summary>Exchange do tipo "topic" onde todos os eventos de chamado sao publicados.</summary>
    public const string ExchangeChamados = "chamados.eventos";

    /// <summary>Fila que o Worker consome.</summary>
    public const string FilaWorker = "helpdesk.worker.chamados";

    /// <summary>"chamado.#" = qualquer routing key que comece com "chamado." (aberto, resolvido, etc.).</summary>
    public const string RoutingKeyTodosOsChamados = "chamado.#";
}
