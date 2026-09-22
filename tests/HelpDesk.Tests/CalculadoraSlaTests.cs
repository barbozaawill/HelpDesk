using HelpDesk.Domain.Enums;
using HelpDesk.Domain.Servicos;

namespace HelpDesk.Tests;

public class CalculadoraSlaTests
{
    private static readonly DateTime Abertura = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(Prioridade.Baixa, 8, 72)]
    [InlineData(Prioridade.Media, 4, 24)]
    [InlineData(Prioridade.Alta, 1, 8)]
    public void Calcular_RetornaPrazosEsperados_EmHoras(Prioridade prioridade, int horasResposta, int horasResolucao)
    {
        var (prazoResposta, prazoResolucao) = CalculadoraSla.Calcular(prioridade, Abertura);

        Assert.Equal(Abertura.AddHours(horasResposta), prazoResposta);
        Assert.Equal(Abertura.AddHours(horasResolucao), prazoResolucao);
    }

    [Fact]
    public void Critica_TemPrimeiraRespostaDe15Minutos()
    {
        var (prazoResposta, _) = CalculadoraSla.Calcular(Prioridade.Critica, Abertura);

        Assert.Equal(Abertura.AddMinutes(15), prazoResposta);
    }

    [Fact]
    public void PrioridadeMaisAlta_TemPrazoMenor()
    {
        var (_, resolucaoBaixa) = CalculadoraSla.Calcular(Prioridade.Baixa, Abertura);
        var (_, resolucaoCritica) = CalculadoraSla.Calcular(Prioridade.Critica, Abertura);

        Assert.True(resolucaoCritica < resolucaoBaixa);
    }
}
