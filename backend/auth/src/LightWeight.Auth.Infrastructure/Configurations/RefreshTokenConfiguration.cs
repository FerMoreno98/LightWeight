using LightWeight.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Auth.Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("auth_RefreshTokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("Id");
        builder.Property(r => r.DeviceTokenId).HasColumnName("DeviceTokenId");
        builder.Property(r => r.Token).HasColumnName("Token").IsRequired();
        builder.Property(r => r.ExpiresAt).HasColumnName("ExpiresAt");
        builder.Property(r => r.RevokedAt).HasColumnName("RevokedAt");
        builder.Property(r => r.ReplacedTokenBy).HasColumnName("ReplacedTokenBy");
        builder.Property(r => r.CreatedByIp).HasColumnName("CreatedByIp").IsRequired();
             builder.Property(x => x.Id)
    .ValueGeneratedNever();

    }
}