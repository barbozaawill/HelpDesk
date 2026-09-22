using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace HelpDesk.Web.Services;

/// <summary>
/// Diz pro Blazor quem esta logado. Le o token JWT do localStorage, quebra o "payload"
/// (a parte do meio do token) e transforma os claims num ClaimsPrincipal.
///
/// Obs: aqui a gente NAO valida a assinatura do token - isso e trabalho da API.
/// O front so usa os claims pra decidir o que mostrar.
/// </summary>
public class ProvedorDeAutenticacao : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonimo =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ILocalStorageService _storage;

    public ProvedorDeAutenticacao(ILocalStorageService storage) => _storage = storage;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetItemAsStringAsync(ClienteApi.ChaveToken);
        if (string.IsNullOrWhiteSpace(token))
            return Anonimo;

        var claims = LerClaims(token).ToList();

        // Token expirado? Descarta.
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (long.TryParse(exp, out var segundos) &&
            DateTimeOffset.FromUnixTimeSeconds(segundos) <= DateTimeOffset.UtcNow)
        {
            await _storage.RemoveItemAsync(ClienteApi.ChaveToken);
            return Anonimo;
        }

        var identidade = new ClaimsIdentity(claims, authenticationType: "jwt",
            nameType: "nome", roleType: ClaimTypes.Role);

        return new AuthenticationState(new ClaimsPrincipal(identidade));
    }

    /// <summary>Chamado depois do login/logout pra a UI reagir na hora.</summary>
    public void AvisarQueMudou() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> LerClaims(string jwt)
    {
        var partes = jwt.Split('.');
        if (partes.Length < 2)
            yield break;

        var json = DecodificarBase64Url(partes[1]);
        var mapa = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (mapa is null)
            yield break;

        foreach (var (chave, valor) in mapa)
        {
            if (valor.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in valor.EnumerateArray())
                    yield return new Claim(chave, item.ToString());
            }
            else
            {
                yield return new Claim(chave, valor.ToString());
            }
        }
    }

    private static byte[] DecodificarBase64Url(string valor)
    {
        valor = valor.Replace('-', '+').Replace('_', '/');
        valor += (valor.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => ""
        };
        return Convert.FromBase64String(valor);
    }
}
