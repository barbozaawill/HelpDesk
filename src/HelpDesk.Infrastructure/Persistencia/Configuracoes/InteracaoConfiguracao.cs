using HelpDesk.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistencia.Configuracoes;

public class InteracaoConfiguracao : IEntityTypeConfiguration<Interacao>
{
    public void Configure(EntityTypeBuilder<Interacao> b)
    {
        b.ToTable("interacoes");
        b.HasKey(i => i.Id);

        b.Property(i => i.Mensagem).HasMaxLength(4000).IsRequired();

        // A relacao com Chamado ja foi configurada em ChamadoConfiguracao.
        b.HasOne(i => i.Autor)
            .WithMany()
            .HasForeignKey(i => i.AutorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(i => i.ChamadoId);
    }
}
