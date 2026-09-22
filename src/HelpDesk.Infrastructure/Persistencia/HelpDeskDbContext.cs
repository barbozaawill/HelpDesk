using HelpDesk.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Persistencia;

/// <summary>
/// O "mapa" entre as entidades do dominio e as tabelas do PostgreSQL.
/// </summary>
public class HelpDeskDbContext : DbContext
{
    public HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Chamado> Chamados => Set<Chamado>();
    public DbSet<Interacao> Interacoes => Set<Interacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Pega todas as classes IEntityTypeConfiguration deste projeto
        // (a pasta Persistencia/Configuracoes).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpDeskDbContext).Assembly);
    }
}
