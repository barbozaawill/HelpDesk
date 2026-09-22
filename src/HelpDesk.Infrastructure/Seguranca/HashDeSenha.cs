namespace HelpDesk.Infrastructure.Seguranca;

/// <summary>
/// Gera e confere o hash da senha. Interface separada da implementacao pra
/// dar pra testar/trocar o algoritmo sem mexer no resto.
/// </summary>
public interface IHashDeSenha
{
    string Gerar(string senha);
    bool Conferir(string senha, string hash);
}

/// <summary>Implementacao com BCrypt (algoritmo proprio pra senha, com "salt" embutido).</summary>
public sealed class HashDeSenhaBCrypt : IHashDeSenha
{
    public string Gerar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public bool Conferir(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
