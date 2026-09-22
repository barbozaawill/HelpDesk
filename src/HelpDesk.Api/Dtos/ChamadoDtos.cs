using HelpDesk.Domain.Enums;

namespace HelpDesk.Api.Dtos;

// ---- Entrada ----

public record AbrirChamadoRequest(string Titulo, string Descricao, int CategoriaId, Prioridade Prioridade);

public record ResponderRequest(string Mensagem, bool Interna = false);

/// <summary>Filtros da listagem (todos opcionais).</summary>
public record FiltroChamados(StatusChamado? Status = null);

// ---- Saida ----

public record ChamadoResumoDto(
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

public record InteracaoDto(
    int Id,
    string Autor,
    int AutorId,
    string Mensagem,
    bool Interna,
    DateTime CriadaEm);

public record ChamadoDetalheDto(
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
    IReadOnlyList<InteracaoDto> Interacoes);
