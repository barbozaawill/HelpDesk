using HelpDesk.Domain.Enums;

namespace HelpDesk.Domain.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash BCrypt da senha. NUNCA guardamos a senha em texto puro.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Cliente;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Atendentes e admins podem trabalhar nos chamados.</summary>
    public bool PodeAtender => Perfil is PerfilUsuario.Atendente or PerfilUsuario.Admin;
}
