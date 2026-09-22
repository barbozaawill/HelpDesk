using HelpDesk.Infrastructure.Cache;
using HelpDesk.Infrastructure.Mensageria;
using HelpDesk.Infrastructure.Persistencia;
using HelpDesk.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HelpDesk.Infrastructure;

/// <summary>
/// Ponto unico onde a infraestrutura (banco, cache, mensageria) e registrada no DI.
/// A API e o Worker chamam so <c>services.AddInfraestrutura(config)</c>.
/// </summary>
public static class ConfiguracaoDeInfraestrutura
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration config)
    {
        AdicionarBanco(services, config);
        AdicionarRedis(services, config);
        AdicionarRabbitMq(services, config);

        services.AddSingleton<IHashDeSenha, HashDeSenhaBCrypt>();

        return services;
    }

    private static void AdicionarBanco(IServiceCollection services, IConfiguration config)
    {
        var connString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' nao configurada (ConnectionStrings:Postgres).");

        services.AddDbContext<HelpDeskDbContext>(opt => opt.UseNpgsql(connString));
    }

    private static void AdicionarRedis(IServiceCollection services, IConfiguration config)
    {
        services.Configure<RedisOpcoes>(config.GetSection(RedisOpcoes.Secao));

        var connString = config.GetSection(RedisOpcoes.Secao)["ConnectionString"] ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opcoes = ConfigurationOptions.Parse(connString);
            // Nao derruba a aplicacao se o Redis ainda nao subiu; ele reconecta sozinho.
            opcoes.AbortOnConnectFail = false;
            opcoes.ConnectRetry = 5;
            return ConnectionMultiplexer.Connect(opcoes);
        });

        services.AddSingleton<ICacheDeLeitura, CacheRedis>();
        services.AddSingleton<IArmazenamentoDeMetricas, ArmazenamentoDeMetricasRedis>();
    }

    private static void AdicionarRabbitMq(IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitMqOpcoes>(config.GetSection(RabbitMqOpcoes.Secao));
        services.AddSingleton<IPublicadorDeEventos, PublicadorDeEventosRabbitMq>();
    }
}
