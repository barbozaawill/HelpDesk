using HelpDesk.Api.Dtos;
using HelpDesk.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly HelpDeskDbContext _db;

    public CategoriasController(HelpDeskDbContext db) => _db = db;

    /// <summary>Categorias ativas (pra preencher o combo do formulario de abertura).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar(CancellationToken ct)
    {
        var categorias = await _db.Categorias
            .AsNoTracking()
            .Where(c => c.Ativa)
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDto(c.Id, c.Nome))
            .ToListAsync(ct);

        return Ok(categorias);
    }
}
