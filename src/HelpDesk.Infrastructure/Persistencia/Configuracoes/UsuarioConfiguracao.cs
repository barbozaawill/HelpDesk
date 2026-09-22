using HelpDesk.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistencia.Configuracoes;

public class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(u => u.Id);

        b.Property(u => u.Nome).HasMaxLength(150).IsRequired();
        b.Property(u => u.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();

        b.Property(u => u.SenhaHash).IsRequired();

        // Guardo o enum como texto ("Admin", "Cliente"...) pra ficar legivel no banco.
        b.Property(u => u.Perfil).HasConversion<string>().HasMaxLength(20);

        b.Ignore(u => u.PodeAtender); // propriedade calculada, nao vira coluna
    }
}
