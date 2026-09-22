using HelpDesk.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Middleware;

/// <summary>
/// Pega exceptions que "vazaram" dos controllers e devolve um JSON de erro decente:
///  - RegraDeNegocioException       -> 400 (culpa do cliente: pediu algo invalido)
///  - RecursoNaoEncontradoException -> 404
///  - qualquer outra                -> 500 (culpa nossa: bug/servico fora do ar)
/// </summary>
public class MiddlewareDeErros
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MiddlewareDeErros> _log;

    public MiddlewareDeErros(RequestDelegate next, ILogger<MiddlewareDeErros> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _next(contexto);
        }
        catch (RegraDeNegocioException ex)
        {
            _log.LogWarning("Regra de negocio violada: {Mensagem}", ex.Message);
            await EscreverErro(contexto, StatusCodes.Status400BadRequest, "Requisicao invalida", ex.Message);
        }
        catch (RecursoNaoEncontradoException ex)
        {
            await EscreverErro(contexto, StatusCodes.Status404NotFound, "Nao encontrado", ex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Erro nao tratado ao processar {Metodo} {Caminho}",
                contexto.Request.Method, contexto.Request.Path);
            await EscreverErro(contexto, StatusCodes.Status500InternalServerError,
                "Erro interno", "Algo deu errado do nosso lado. Tente novamente mais tarde.");
        }
    }

    private static async Task EscreverErro(HttpContext contexto, int status, string titulo, string detalhe)
    {
        if (contexto.Response.HasStarted)
            return;

        contexto.Response.Clear();
        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe
        });
    }
}
