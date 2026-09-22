using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HelpDesk.Api.Configuracao;
using HelpDesk.Domain.Entidades;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HelpDesk.Api.Autenticacao;

/// <summary>Monta o token JWT que o Blazor guarda e manda em cada request.</summary>
public class ServicoDeToken
{
    private readonly OpcoesJwt _opcoes;

    public ServicoDeToken(IOptions<OpcoesJwt> opcoes) => _opcoes = opcoes.Value;

    public (string Token, DateTime ExpiraEm) Gerar(Usuario usuario)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_opcoes.MinutosDeValidade);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new("nome", usuario.Nome),
            new(ClaimTypes.Role, usuario.Perfil.ToString()),
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.ChaveSecreta));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opcoes.Emissor,
            audience: _opcoes.Audiencia,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEm,
            signingCredentials: credenciais);

        var texto = new JwtSecurityTokenHandler().WriteToken(token);
        return (texto, expiraEm);
    }
}
