namespace HelpDesk.Api.Dtos;

public record CategoriaDto(int Id, string Nome);

public record MetricasDto(
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
