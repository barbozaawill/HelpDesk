using HelpDesk.Api.Autenticacao;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Servicos;
using HelpDesk.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/chamados")]
[Authorize] // tudo aqui exige estar logado
public class ChamadosController : ControllerBase
{
    private readonly ServicoDeChamados _servico;

    public ChamadosController(ServicoDeChamados servico) => _servico = servico;

    // Atalho: usuario logado montado a partir dos claims do token.
    private UsuarioAtual Usuario => User.ParaUsuarioAtual();

    /// <summary>Lista os chamados. Cliente ve so os seus; atendente/admin veem todos. Usa cache-aside.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChamadoResumoDto>>> Listar(
        [FromQuery] StatusChamado? status, CancellationToken ct)
        => Ok(await _servico.ListarAsync(new FiltroChamados(status), Usuario, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChamadoDetalheDto>> Detalhe(int id, CancellationToken ct)
        => Ok(await _servico.ObterDetalheAsync(id, Usuario, ct));

    /// <summary>Abre um novo chamado (qualquer usuario logado).</summary>
    [HttpPost]
    public async Task<ActionResult<ChamadoDetalheDto>> Abrir(AbrirChamadoRequest req, CancellationToken ct)
    {
        var criado = await _servico.AbrirAsync(req, Usuario, ct);
        return CreatedAtAction(nameof(Detalhe), new { id = criado.Id }, criado);
    }

    /// <summary>Atendente assume o chamado.</summary>
    [HttpPut("{id:int}/assumir")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Atendente)},{nameof(PerfilUsuario.Admin)}")]
    public async Task<ActionResult<ChamadoDetalheDto>> Assumir(int id, CancellationToken ct)
        => Ok(await _servico.AssumirAsync(id, Usuario, ct));

    /// <summary>Adiciona uma resposta/interacao ao chamado.</summary>
    [HttpPut("{id:int}/responder")]
    public async Task<ActionResult<ChamadoDetalheDto>> Responder(int id, ResponderRequest req, CancellationToken ct)
        => Ok(await _servico.ResponderAsync(id, req, Usuario, ct));

    /// <summary>Marca o chamado como resolvido.</summary>
    [HttpPut("{id:int}/resolver")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Atendente)},{nameof(PerfilUsuario.Admin)}")]
    public async Task<ActionResult<ChamadoDetalheDto>> Resolver(int id, CancellationToken ct)
        => Ok(await _servico.ResolverAsync(id, Usuario, ct));

    /// <summary>Fecha o chamado (solicitante ou atendente).</summary>
    [HttpPut("{id:int}/fechar")]
    public async Task<ActionResult<ChamadoDetalheDto>> Fechar(int id, CancellationToken ct)
        => Ok(await _servico.FecharAsync(id, Usuario, ct));

    /// <summary>Reabre um chamado resolvido/fechado.</summary>
    [HttpPut("{id:int}/reabrir")]
    public async Task<ActionResult<ChamadoDetalheDto>> Reabrir(int id, CancellationToken ct)
        => Ok(await _servico.ReabrirAsync(id, Usuario, ct));
}
