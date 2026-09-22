using HelpDesk.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistencia.Configuracoes;

public class CategoriaConfiguracao : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> b)
    {
        b.ToTable("categorias");
        b.HasKey(c => c.Id);
        b.Property(c => c.Nome).HasMaxLength(80).IsRequired();
        b.HasIndex(c => c.Nome).IsUnique();
    }
}
