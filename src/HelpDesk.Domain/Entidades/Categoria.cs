namespace HelpDesk.Domain.Entidades;

/// <summary>
/// Categoria do chamado: Hardware, Software, Rede, Acesso... (tabela de apoio).
/// </summary>
public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;

    public ICollection<Chamado> Chamados { get; set; } = new List<Chamado>();
}
