using Blazored.LocalStorage;
using HelpDesk.Web.Models;

namespace HelpDesk.Web.Services;

/// <summary>
/// Fluxos de login/cadastro/logout: chama a API, guarda o token no localStorage
/// e avisa o <see cref="ProvedorDeAutenticacao"/> pra a UI atualizar.
/// </summary>
public class ServicoDeSessao
{
    private readonly ClienteApi _api;
    private readonly ILocalStorageService _storage;
    private readonly ProvedorDeAutenticacao _provedor;

    public ServicoDeSessao(ClienteApi api, ILocalStorageService storage, ProvedorDeAutenticacao provedor)
    {
        _api = api;
        _storage = storage;
        _provedor = provedor;
    }

    public async Task<UsuarioResumo> EntrarAsync(string email, string senha)
    {
        var resposta = await _api.LoginAsync(new LoginRequest(email, senha));
        await GuardarSessao(resposta);
        return resposta.Usuario;
    }

    public async Task<UsuarioResumo> CadastrarAsync(string nome, string email, string senha)
    {
        var resposta = await _api.RegistrarAsync(new RegistrarClienteRequest(nome, email, senha));
        await GuardarSessao(resposta);
        return resposta.Usuario;
    }

    public async Task SairAsync()
    {
        await _storage.RemoveItemAsync(ClienteApi.ChaveToken);
        _provedor.AvisarQueMudou();
    }

    private async Task GuardarSessao(RespostaLogin resposta)
    {
        await _storage.SetItemAsStringAsync(ClienteApi.ChaveToken, resposta.Token);
        _provedor.AvisarQueMudou();
    }
}
