using LightWeight.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Auth.Infrastructure.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("auth_OtpCodes");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("Id");
        builder.Property(o => o.UserId).HasColumnName("UserId");
        builder.Property(o => o.Hash).HasColumnName("Hash").IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(o => o.ExpiresAt).HasColumnName("ExpiresAt");
        builder.Property(o => o.UsedAt).HasColumnName("UsedAt");
        builder.Property(x => x.Id)
    .ValueGeneratedNever(); // Cuando generas el id en el codigo, si no pones esto en muchas ocasiones EFCore se lia y piensa que la entidad existe

    }
}