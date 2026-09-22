using HelpDesk.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistencia.Configuracoes;

public class ChamadoConfiguracao : IEntityTypeConfiguration<Chamado>
{
    public void Configure(EntityTypeBuilder<Chamado> b)
    {
        b.ToTable("chamados");
        b.HasKey(c => c.Id);

        b.Property(c => c.Titulo).HasMaxLength(160).IsRequired();
        b.Property(c => c.Descricao).HasMaxLength(4000).IsRequired();

        b.Property(c => c.Status).HasConversion<string>().HasMaxLength(25);
        b.Property(c => c.Prioridade).HasConversion<string>().HasMaxLength(15);

        // Relacionamentos. Restrict = nao deixa apagar um usuario/categoria que tem chamado.
        b.HasOne(c => c.Categoria)
            .WithMany(cat => cat.Chamados)
            .HasForeignKey(c => c.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(c => c.Solicitante)
            .WithMany()
            .HasForeignKey(c => c.SolicitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(c => c.Atendente)
            .WithMany()
            .HasForeignKey(c => c.AtendenteId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(c => c.Interacoes)
            .WithOne(i => i.Chamado!)
            .HasForeignKey(i => i.ChamadoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indices pros filtros mais comuns da listagem.
        b.HasIndex(c => c.Status);
        b.HasIndex(c => c.SolicitanteId);
        b.HasIndex(c => c.AtendenteId);

        b.Ignore(c => c.SlaResolucaoEstourado);
        b.Ignore(c => c.TempoDeResolucao);
    }
}
