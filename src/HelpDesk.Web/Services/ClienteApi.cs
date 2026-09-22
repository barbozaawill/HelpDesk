using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using HelpDesk.Web.Models;

namespace HelpDesk.Web.Services;

/// <summary>Erro vindo da API (ja com a mensagem amigavel pronta pra mostrar na tela).</summary>
public class ApiException : Exception
{
    public ApiException(string mensagem) : base(mensagem) { }
}

/// <summary>
/// Cliente HTTP da API. Antes de cada chamada coloca o token JWT no header Authorization
/// (o Blazor WebAssembly roda numa thread so, entao mexer no DefaultRequestHeaders aqui e ok).
/// </summary>
public class ClienteApi
{
    public const string ChaveToken = "helpdesk_token";

    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;

    public ClienteApi(HttpClient http, ILocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    // ---- Autenticacao ----

    public Task<RespostaLogin> LoginAsync(LoginRequest req) =>
        EnviarAsync<RespostaLogin>(HttpMethod.Post, "api/auth/login", req, comToken: false);

    public Task<RespostaLogin> RegistrarAsync(RegistrarClienteRequest req) =>
        EnviarAsync<RespostaLogin>(HttpMethod.Post, "api/auth/registrar", req, comToken: false);

    // ---- Categorias ----

    public async Task<List<CategoriaItem>> ListarCategoriasAsync() =>
        await EnviarAsync<List<CategoriaItem>>(HttpMethod.Get, "api/categorias") ?? new();

    // ---- Chamados ----

    public async Task<List<ChamadoResumo>> ListarChamadosAsync(string? status = null)
    {
        var url = "api/chamados";
        if (!string.IsNullOrWhiteSpace(status))
            url += $"?status={status}";
        return await EnviarAsync<List<ChamadoResumo>>(HttpMethod.Get, url) ?? new();
    }

    public Task<ChamadoDetalhe> ObterChamadoAsync(int id) =>
        EnviarAsync<ChamadoDetalhe>(HttpMethod.Get, $"api/chamados/{id}");

    public Task<ChamadoDetalhe> AbrirChamadoAsync(AbrirChamadoRequest req) =>
        EnviarAsync<ChamadoDetalhe>(HttpMethod.Post, "api/chamados", req);

    public Task<ChamadoDetalhe> AssumirAsync(int id) =>
        EnviarAsync<ChamadoDetalhe>(HttpMethod.Put, $"api/chamados/{id}/assumir");

    public Task<ChamadoDetalhe> ResponderAsync(int id, ResponderRequest req) =>
        EnviarAsync<ChamadoDetalhe>(HttpMethod.Put, $"api/chamados/{id}/responder", req);

    public Task<ChamadoDetalhe> ResolverAsync(int id) =>
        EnviarAsync<ChamadoDetalhe>(HttpMethod.Put, $"api/chamados/{id}/resolver");

    public Task<ChamadoDetalhe> FecharAsync(int id) =>
        EnviarAsync<ChamadoDetalhe>(HttpMethod.Put, $"api/chamados/{id}/fechar");

    public Task<ChamadoDetalhe> ReabrirAsync(int id) =>
        EnviarAsync<ChamadoDetalhe>(HttpMethod.Put, $"api/chamados/{id}/reabrir");

    // ---- Dashboard ----

    public Task<Metricas> ObterMetricasAsync() =>
        EnviarAsync<Metricas>(HttpMethod.Get, "api/dashboard/metricas");

    // ------------------------------------------------------------------

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string url, object? corpo = null, bool comToken = true)
    {
        using var requisicao = new HttpRequestMessage(metodo, url);

        if (corpo is not null)
            requisicao.Content = JsonContent.Create(corpo);

        if (comToken)
        {
            var token = await _storage.GetItemAsStringAsync(ChaveToken);
            if (!string.IsNullOrWhiteSpace(token))
                requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage resposta;
        try
        {
            resposta = await _http.SendAsync(requisicao);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("Nao consegui falar com a API. Ela esta rodando?");
        }

        if (resposta.IsSuccessStatusCode)
        {
            if (typeof(T) == typeof(bool)) return default!;
            return (await resposta.Content.ReadFromJsonAsync<T>())!;
        }

        if (resposta.StatusCode == HttpStatusCode.Unauthorized)
            throw new ApiException("Sua sessao expirou. Faca login de novo.");

        throw new ApiException(await LerMensagemDeErroAsync(resposta));
    }

    private static async Task<string> LerMensagemDeErroAsync(HttpResponseMessage resposta)
    {
        try
        {
            var problema = await resposta.Content.ReadFromJsonAsync<ProblemaApi>();
            if (!string.IsNullOrWhiteSpace(problema?.Detail)) return problema!.Detail!;
            if (!string.IsNullOrWhiteSpace(problema?.Title)) return problema!.Title!;
        }
        catch
        {
            // resposta nao era JSON de ProblemDetails
        }
        return $"Erro {(int)resposta.StatusCode} ao chamar a API.";
    }
}
