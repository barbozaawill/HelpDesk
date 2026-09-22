namespace HelpDesk.Web.Helpers;

/// <summary>Cores de badge do Bootstrap por status/prioridade (so pra deixar a tela mais legivel).</summary>
public static class Estilos
{
    public static string BadgeStatus(string status) => status switch
    {
        "Aberto" => "bg-primary",
        "Reaberto" => "bg-primary",
        "EmAtendimento" => "bg-info text-dark",
        "AguardandoCliente" => "bg-warning text-dark",
        "Resolvido" => "bg-success",
        "Fechado" => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string BadgePrioridade(string prioridade) => prioridade switch
    {
        "Baixa" => "bg-success",
        "Media" => "bg-info text-dark",
        "Alta" => "bg-warning text-dark",
        "Critica" => "bg-danger",
        _ => "bg-light text-dark"
    };

    public static string TextoStatus(string status) => status switch
    {
        "EmAtendimento" => "Em atendimento",
        "AguardandoCliente" => "Aguardando cliente",
        _ => status
    };
}
