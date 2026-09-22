namespace HelpDesk.Api.Dtos;

// DTOs = objetos "de transporte" da API. Ficam separados das entidades do dominio
// pra gente controlar exatamente o que entra e o que sai pela rede.

public record LoginRequest(string Email, string Senha);

public record RegistrarClienteRequest(string Nome, string Email, string Senha);

public record UsuarioDto(int Id, string Nome, string Email, string Perfil);

public record RespostaLogin(string Token, DateTime ExpiraEm, UsuarioDto Usuario);
