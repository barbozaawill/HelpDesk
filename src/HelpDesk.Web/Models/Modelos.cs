using HelpDesk.Domain.Enums;

namespace HelpDesk.Web.Models;

// Espelham o que a API devolve/recebe. Ficam aqui pra o front nao depender dos DTOs da API.

public record UsuarioResumo(int Id, string Nome, string Email, string Perfil);

public record RespostaLogin(string Token, DateTime ExpiraEm, UsuarioResumo Usuario);

public record CategoriaItem(int Id, string Nome);

public record ChamadoResumo(
    int Id,
    string Titulo,
    string Status,
    string Prioridade,
    string Categoria,
    string Solicitante,
    string? Atendente,
    DateTime CriadoEm,
    DateTime PrazoResolucao,
    bool SlaEstourado);

public record InteracaoItem(
    int Id,
    string Autor,
    int AutorId,
    string Mensagem,
    bool Interna,
    DateTime CriadaEm);

public record ChamadoDetalhe(
    int Id,
    string Titulo,
    string Descricao,
    string Status,
    string Prioridade,
    int CategoriaId,
    string Categoria,
    int SolicitanteId,
    string Solicitante,
    int? AtendenteId,
    string? Atendente,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    DateTime? ResolvidoEm,
    DateTime? FechadoEm,
    DateTime PrazoPrimeiraResposta,
    DateTime PrazoResolucao,
    bool SlaEstourado,
    List<InteracaoItem> Interacoes);

public record Metricas(
    int Abertos,
    int EmAtendimento,
    int AguardandoCliente,
    int ResolvidosNoDia,
    int FechadosNoDia,
    int SlaEstourado,
    int TotalAtivos,
    double HorasMediasResolucao,
    DateTime? AtualizadoEm,
    string Origem);

// ---- Entradas de formulario ----

public class NovoChamadoForm
{
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public Prioridade Prioridade { get; set; } = Prioridade.Media;
}

public record AbrirChamadoRequest(string Titulo, string Descricao, int CategoriaId, Prioridade Prioridade);
public record LoginRequest(string Email, string Senha);
public record RegistrarClienteRequest(string Nome, string Email, string Senha);
public record ResponderRequest(string Mensagem, bool Interna);

/// <summary>Formato de erro que a API devolve (ProblemDetails).</summary>
public record ProblemaApi(string? Title, string? Detail, int? Status);
