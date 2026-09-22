namespace HelpDesk.Domain.Common;

/// <summary>
/// Erro de regra de negocio (ex: tentar fechar um chamado que ja esta fechado).
/// A API tem um middleware que traduz isso pra um HTTP 400, em vez de deixar
/// virar um 500 "Internal Server Error".
/// </summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem)
    {
    }
}
