namespace HelpDesk.Domain.Entidades;

/// <summary>
/// Uma mensagem dentro de um chamado (o historico da conversa).
/// </summary>
public class Interacao
{
    public int Id { get; set; }

    public int ChamadoId { get; set; }
    public Chamado? Chamado { get; set; }

    public int AutorId { get; set; }
    public Usuario? Autor { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    /// <summary>Nota interna entre atendentes: nao deve ser exibida pro cliente.</summary>
    public bool Interna { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}
