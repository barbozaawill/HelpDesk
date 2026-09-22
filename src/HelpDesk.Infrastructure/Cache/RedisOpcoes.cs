namespace HelpDesk.Infrastructure.Cache;

/// <summary>Configuracao do Redis (appsettings, secao "Redis").</summary>
public class RedisOpcoes
{
    public const string Secao = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>TTL padrao (em segundos) do cache-aside da listagem de chamados.</summary>
    public int SegundosDeCachePadrao { get; set; } = 60;
}
