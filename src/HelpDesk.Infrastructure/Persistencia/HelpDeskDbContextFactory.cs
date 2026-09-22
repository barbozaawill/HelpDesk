using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HelpDesk.Infrastructure.Persistencia;

/// <summary>
/// Usado SOMENTE pelo "dotnet ef" na hora de criar/aplicar migrations em tempo de design.
/// Em runtime quem configura o DbContext e o Program.cs da API/Worker.
///
/// Ter esta classe evita que o "dotnet ef" precise subir a aplicacao inteira
/// (e o RabbitMQ, o Redis...) so pra gerar um arquivo de migration.
/// </summary>
public class HelpDeskDbContextFactory : IDesignTimeDbContextFactory<HelpDeskDbContext>
{
    public HelpDeskDbContext CreateDbContext(string[] args)
    {
        var connString = Environment.GetEnvironmentVariable("HELPDESK_MIGRATIONS_CONN")
            ?? "Host=localhost;Port=5432;Database=helpdesk;Username=helpdesk;Password=helpdesk";

        var options = new DbContextOptionsBuilder<HelpDeskDbContext>()
            .UseNpgsql(connString, npg =>
                npg.MigrationsAssembly(typeof(HelpDeskDbContextFactory).Assembly.GetName().Name))
            .Options;

        return new HelpDeskDbContext(options);
    }
}
