namespace HelpDesk.Domain.Common;

/// <summary>
/// Lancada quando um recurso pedido nao existe (ou o usuario nao tem permissao de ve-lo).
/// A API traduz pra HTTP 404.
/// </summary>
public class RecursoNaoEncontradoException : Exception
{
    public RecursoNaoEncontradoException(string mensagem) : base(mensagem)
    {
    }

    public static RecursoNaoEncontradoException Chamado(int id) =>
        new($"Chamado #{id} nao encontrado.");
}
