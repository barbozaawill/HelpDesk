using HelpDesk.Domain.Common;
using HelpDesk.Domain.Entidades;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Tests;

/// <summary>Testes da maquina de estados do chamado (so dominio, sem banco).</summary>
public class ChamadoTests
{
    private const int Cliente = 1;
    private const int Atendente = 99;

    private static Chamado NovoChamado() =>
        new("Nao liga", "O computador nao liga de jeito nenhum", categoriaId: 1, Prioridade.Media, Cliente);

    [Fact]
    public void RecemCriado_ComecaComoAberto()
    {
        var chamado = NovoChamado();

        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Null(chamado.AtendenteId);
    }

    [Fact]
    public void Construtor_SemTitulo_Lanca()
    {
        Assert.Throws<RegraDeNegocioException>(() =>
            new Chamado("  ", "descricao", 1, Prioridade.Baixa, Cliente));
    }

    [Fact]
    public void Assumir_QuandoAberto_VaiParaEmAtendimento()
    {
        var chamado = NovoChamado();

        chamado.Assumir(Atendente);

        Assert.Equal(StatusChamado.EmAtendimento, chamado.Status);
        Assert.Equal(Atendente, chamado.AtendenteId);
    }

    [Fact]
    public void Assumir_DuasVezes_Lanca()
    {
        var chamado = NovoChamado();
        chamado.Assumir(Atendente);

        Assert.Throws<RegraDeNegocioException>(() => chamado.Assumir(Atendente));
    }

    [Fact]
    public void Resolver_SemEstarEmAtendimento_Lanca()
    {
        var chamado = NovoChamado();

        Assert.Throws<RegraDeNegocioException>(() => chamado.Resolver());
    }

    [Fact]
    public void AtendenteResponde_ColocaEmAguardandoCliente()
    {
        var chamado = NovoChamado();
        chamado.Assumir(Atendente);

        chamado.AdicionarInteracao(Atendente, "Voce ja testou outra tomada?", autorEhAtendente: true);

        Assert.Equal(StatusChamado.AguardandoCliente, chamado.Status);
        Assert.True(chamado.PrimeiraRespostaRegistrada);
    }

    [Fact]
    public void ClienteResponde_DevolveParaEmAtendimento()
    {
        var chamado = NovoChamado();
        chamado.Assumir(Atendente);
        chamado.AdicionarInteracao(Atendente, "Testou outra tomada?", autorEhAtendente: true);

        chamado.AdicionarInteracao(Cliente, "Testei, continua igual", autorEhAtendente: false);

        Assert.Equal(StatusChamado.EmAtendimento, chamado.Status);
    }

    [Fact]
    public void NotaInterna_NaoMudaOStatus()
    {
        var chamado = NovoChamado();
        chamado.Assumir(Atendente);

        chamado.AdicionarInteracao(Atendente, "Provavel fonte queimada", autorEhAtendente: true, interna: true);

        Assert.Equal(StatusChamado.EmAtendimento, chamado.Status);
    }

    [Fact]
    public void FluxoCompleto_DeAbertoAteFechado()
    {
        var chamado = NovoChamado();

        chamado.Assumir(Atendente);
        chamado.Resolver();
        chamado.Fechar();

        Assert.Equal(StatusChamado.Fechado, chamado.Status);
        Assert.NotNull(chamado.ResolvidoEm);
        Assert.NotNull(chamado.FechadoEm);
        Assert.NotNull(chamado.TempoDeResolucao);
    }

    [Fact]
    public void Fechar_SemResolverAntes_Lanca()
    {
        var chamado = NovoChamado();
        chamado.Assumir(Atendente);

        Assert.Throws<RegraDeNegocioException>(() => chamado.Fechar());
    }

    [Fact]
    public void Reabrir_ChamadoFechado_VoltaParaReaberto()
    {
        var chamado = NovoChamado();
        chamado.Assumir(Atendente);
        chamado.Resolver();
        chamado.Fechar();

        chamado.Reabrir();

        Assert.Equal(StatusChamado.Reaberto, chamado.Status);
        Assert.Null(chamado.ResolvidoEm);
        Assert.Null(chamado.FechadoEm);
    }

    [Fact]
    public void ChamadoFechado_NaoAceitaNovaInteracao()
    {
        var chamado = NovoChamado();
        chamado.Assumir(Atendente);
        chamado.Resolver();
        chamado.Fechar();

        Assert.Throws<RegraDeNegocioException>(() =>
            chamado.AdicionarInteracao(Cliente, "ainda com problema", autorEhAtendente: false));
    }
}
