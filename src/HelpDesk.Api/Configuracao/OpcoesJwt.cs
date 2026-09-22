namespace HelpDesk.Api.Configuracao;

/// <summary>Configuracao do token JWT (appsettings, secao "Jwt").</summary>
public class OpcoesJwt
{
    public const string Secao = "Jwt";

    /// <summary>Chave usada pra assinar o token. Em producao viria de um cofre de segredos.</summary>
    public string ChaveSecreta { get; set; } = string.Empty;

    public string Emissor { get; set; } = "HelpDesk.Api";
    public string Audiencia { get; set; } = "HelpDesk.Web";
    public int MinutosDeValidade { get; set; } = 480; // 8 horas
}
