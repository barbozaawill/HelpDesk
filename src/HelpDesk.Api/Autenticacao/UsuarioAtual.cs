using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Api.Autenticacao;

/// <summary>
/// Dados do usuario logado, extraidos do token. Os controllers montam isso a partir
/// dos claims e passam pros servicos (assim o servico nao depende de HttpContext).
/// </summary>
public record UsuarioAtual(int Id, string Nome, PerfilUsuario Perfil)
{
    public bool EhAtendente => Perfil is PerfilUsuario.Atendente or PerfilUsuario.Admin;
}

public static class ClaimsPrincipalExtensions
{
    public static UsuarioAtual ParaUsuarioAtual(this ClaimsPrincipal principal)
    {
        var id = principal.ObterId();
        var nome = principal.FindFirst("nome")?.Value ?? "Usuario";
        var perfilTexto = principal.FindFirst(ClaimTypes.Role)?.Value ?? nameof(PerfilUsuario.Cliente);
        Enum.TryParse<PerfilUsuario>(perfilTexto, out var perfil);
        return new UsuarioAtual(id, nome, perfil);
    }

    public static int ObterId(this ClaimsPrincipal principal)
    {
        // Dependendo do mapeamento de claims, o id pode vir como "sub" ou como NameIdentifier.
        var valor = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(valor, out var id)
            ? id
            : throw new InvalidOperationException("Token nao contem um id de usuario valido.");
    }
}
