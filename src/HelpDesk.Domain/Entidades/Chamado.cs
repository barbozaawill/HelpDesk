using HelpDesk.Domain.Common;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Domain.Entidades;

/// <summary>
/// O chamado (ticket) de suporte. Coracao do dominio.
///
/// Coloquei as mudancas de estado em METODOS (Assumir, Resolver, Fechar...) em vez de
/// deixar o "Status" com set publico. Assim ninguem consegue pular etapas da maquina de
/// estados por acidente: cada transicao invalida vira um <see cref="RegraDeNegocioException"/>.
/// </summary>
public class Chamado
{
    // EF Core usa este construtor sem parametros pra recriar o objeto vindo do banco.
    private Chamado() { }

    public Chamado(string titulo, string descricao, int categoriaId, Prioridade prioridade, int solicitanteId)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new RegraDeNegocioException("O titulo do chamado e obrigatorio.");
        if (string.IsNullOrWhiteSpace(descricao))
            throw new RegraDeNegocioException("A descricao do chamado e obrigatoria.");

        Titulo = titulo.Trim();
        Descricao = descricao.Trim();
        CategoriaId = categoriaId;
        Prioridade = prioridade;
        SolicitanteId = solicitanteId;

        Status = StatusChamado.Aberto;
        CriadoEm = DateTime.UtcNow;
        AtualizadoEm = CriadoEm;
    }

    public int Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;

    public StatusChamado Status { get; private set; }
    public Prioridade Prioridade { get; private set; }

    public int CategoriaId { get; private set; }
    public Categoria? Categoria { get; private set; }

    // Quem abriu o chamado.
    public int SolicitanteId { get; private set; }
    public Usuario? Solicitante { get; private set; }

    // Atendente responsavel (fica nulo enquanto ninguem assume).
    public int? AtendenteId { get; private set; }
    public Usuario? Atendente { get; private set; }

    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
    public DateTime? ResolvidoEm { get; private set; }
    public DateTime? FechadoEm { get; private set; }

    // Prazos de SLA, calculados no momento da abertura a partir da prioridade.
    public DateTime PrazoPrimeiraResposta { get; private set; }
    public DateTime PrazoResolucao { get; private set; }
    public bool PrimeiraRespostaRegistrada { get; private set; }

    public List<Interacao> Interacoes { get; private set; } = new();

    /// <summary>True quando o prazo de resolucao ja passou e o chamado ainda esta em aberto.</summary>
    public bool SlaResolucaoEstourado =>
        Status is not (StatusChamado.Resolvido or StatusChamado.Fechado)
        && DateTime.UtcNow > PrazoResolucao;

    /// <summary>Tempo entre abertura e resolucao (null se ainda nao foi resolvido).</summary>
    public TimeSpan? TempoDeResolucao =>
        ResolvidoEm is null ? null : ResolvidoEm.Value - CriadoEm;

    public void DefinirPrazosSla(DateTime prazoPrimeiraResposta, DateTime prazoResolucao)
    {
        PrazoPrimeiraResposta = prazoPrimeiraResposta;
        PrazoResolucao = prazoResolucao;
    }

    // --- Transicoes da maquina de estados -------------------------------------

    public void Assumir(int atendenteId)
    {
        if (Status is not (StatusChamado.Aberto or StatusChamado.Reaberto))
            throw new RegraDeNegocioException(
                $"So da pra assumir um chamado Aberto ou Reaberto (status atual: {Status}).");

        AtendenteId = atendenteId;
        Status = StatusChamado.EmAtendimento;
        Tocar();
    }

    public Interacao AdicionarInteracao(int autorId, string mensagem, bool autorEhAtendente, bool interna = false)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
            throw new RegraDeNegocioException("A mensagem nao pode ser vazia.");
        if (Status is StatusChamado.Fechado)
            throw new RegraDeNegocioException("Chamado fechado nao aceita respostas. Reabra o chamado primeiro.");

        var interacao = new Interacao
        {
            ChamadoId = Id,
            AutorId = autorId,
            Mensagem = mensagem.Trim(),
            Interna = interna,
            CriadaEm = DateTime.UtcNow
        };
        Interacoes.Add(interacao);

        // Regrinhas da maquina de estados quando alguem responde:
        if (autorEhAtendente && !interna)
        {
            PrimeiraRespostaRegistrada = true;
            if (Status is StatusChamado.EmAtendimento or StatusChamado.Reaberto)
                Status = StatusChamado.AguardandoCliente;
        }
        else if (!autorEhAtendente && Status is StatusChamado.AguardandoCliente)
        {
            // Cliente respondeu: a bola volta pro atendente.
            Status = StatusChamado.EmAtendimento;
        }

        Tocar();
        return interacao;
    }

    public void Resolver()
    {
        if (Status is not (StatusChamado.EmAtendimento or StatusChamado.AguardandoCliente or StatusChamado.Reaberto))
            throw new RegraDeNegocioException(
                $"So da pra resolver um chamado que esta em atendimento (status atual: {Status}).");

        Status = StatusChamado.Resolvido;
        ResolvidoEm = DateTime.UtcNow;
        Tocar();
    }

    public void Fechar()
    {
        if (Status is StatusChamado.Fechado)
            throw new RegraDeNegocioException("O chamado ja esta fechado.");
        if (Status is not StatusChamado.Resolvido)
            throw new RegraDeNegocioException("Um chamado so pode ser fechado depois de Resolvido.");

        Status = StatusChamado.Fechado;
        FechadoEm = DateTime.UtcNow;
        Tocar();
    }

    public void Reabrir()
    {
        if (Status is not (StatusChamado.Resolvido or StatusChamado.Fechado))
            throw new RegraDeNegocioException("So da pra reabrir um chamado Resolvido ou Fechado.");

        Status = StatusChamado.Reaberto;
        ResolvidoEm = null;
        FechadoEm = null;
        Tocar();
    }

    private void Tocar() => AtualizadoEm = DateTime.UtcNow;
}
