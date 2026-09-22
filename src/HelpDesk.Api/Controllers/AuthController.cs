using System.IdentityModel.Tokens.Jwt;
using HelpDesk.Api.Autenticacao;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ServicoDeAutenticacao _auth;

    public AuthController(ServicoDeAutenticacao auth) => _auth = auth;

    /// <summary>Login. Devolve o token JWT que deve ser enviado no header Authorization.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<RespostaLogin>> Login(LoginRequest req, CancellationToken ct)
        => Ok(await _auth.LoginAsync(req, ct));

    /// <summary>Auto-cadastro de cliente.</summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    public async Task<ActionResult<RespostaLogin>> Registrar(RegistrarClienteRequest req, CancellationToken ct)
        => Ok(await _auth.RegistrarClienteAsync(req, ct));

    /// <summary>Dados do usuario logado (util pro front conferir o token).</summary>
    [HttpGet("eu")]
    [Authorize]
    public ActionResult<UsuarioDto> Eu()
    {
        var u = User.ParaUsuarioAtual();
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? "";
        return Ok(new UsuarioDto(u.Id, u.Nome, email, u.Perfil.ToString()));
    }
}
