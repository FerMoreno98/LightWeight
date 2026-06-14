
using LightWeight.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Auth.Infrastructure.Configurations;
public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("auth_DeviceTokens");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("Id");
        builder.Property(d => d.UserId).HasColumnName("UserId");
        builder.Property(d => d.DeviceIdentifier).HasColumnName("DeviceIdentifier").IsRequired();
        builder.Property(d => d.DeviceName).HasColumnName("DeviceName").IsRequired();
        builder.Property(d => d.Platform).HasColumnName("Platform").IsRequired();
        builder.Property(d => d.LastSeenAt).HasColumnName("LastSeenAt");
        builder.Property(d => d.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(d => d.RevokedAt).HasColumnName("RevokedAt");
        builder.Property(x => x.Id)
    .ValueGeneratedNever();

        builder.HasMany(d => d.RefreshTokens)
            .WithOne()
            .HasForeignKey(r => r.DeviceTokenId)
            .HasPrincipalKey(d => d.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}