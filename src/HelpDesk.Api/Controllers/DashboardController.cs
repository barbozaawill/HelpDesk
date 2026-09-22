using HelpDesk.Api.Dtos;
using HelpDesk.Api.Servicos;
using HelpDesk.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = $"{nameof(PerfilUsuario.Atendente)},{nameof(PerfilUsuario.Admin)}")]
public class DashboardController : ControllerBase
{
    private readonly ServicoDeDashboard _dashboard;

    public DashboardController(ServicoDeDashboard dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Contadores do dashboard. Le direto do Redis (valores prontos, mantidos pelo Worker).
    /// </summary>
    [HttpGet("metricas")]
    public async Task<ActionResult<MetricasDto>> Metricas(CancellationToken ct)
        => Ok(await _dashboard.ObterAsync(ct));
}
