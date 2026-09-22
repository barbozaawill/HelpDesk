using HelpDesk.Api.Autenticacao;
using HelpDesk.Api.Dtos;
using HelpDesk.Domain.Common;
using HelpDesk.Domain.Entidades;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Persistencia;
using HelpDesk.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Servicos;

public class ServicoDeAutenticacao
{
    private readonly HelpDeskDbContext _db;
    private readonly IHashDeSenha _hash;
    private readonly ServicoDeToken _token;

    public ServicoDeAutenticacao(HelpDeskDbContext db, IHashDeSenha hash, ServicoDeToken token)
    {
        _db = db;
        _hash = hash;
        _token = token;
    }

    public async Task<RespostaLogin> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Mesma mensagem pros dois casos (usuario nao existe / senha errada): nao entrega
        // pra um atacante se o e-mail esta ou nao cadastrado.
        if (usuario is null || !_hash.Conferir(req.Senha ?? string.Empty, usuario.SenhaHash))
            throw new RegraDeNegocioException("E-mail ou senha invalidos.");

        return MontarResposta(usuario);
    }

    public async Task<RespostaLogin> RegistrarClienteAsync(RegistrarClienteRequest req, CancellationToken ct)
    {
        var nome = (req.Nome ?? string.Empty).Trim();
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        var senha = req.Senha ?? string.Empty;

        if (nome.Length < 2)
            throw new RegraDeNegocioException("Informe seu nome.");
        if (!email.Contains('@'))
            throw new RegraDeNegocioException("E-mail invalido.");
        if (senha.Length < 6)
            throw new RegraDeNegocioException("A senha precisa ter pelo menos 6 caracteres.");

        if (await _db.Usuarios.AnyAsync(u => u.Email == email, ct))
            throw new RegraDeNegocioException("Ja existe uma conta com esse e-mail.");

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Perfil = PerfilUsuario.Cliente, // auto-cadastro so cria cliente
            SenhaHash = _hash.Gerar(senha)
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);

        return MontarResposta(usuario);
    }

    private RespostaLogin MontarResposta(Usuario usuario)
    {
        var (token, expiraEm) = _token.Gerar(usuario);
        var dto = new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil.ToString());
        return new RespostaLogin(token, expiraEm, dto);
    }
}
