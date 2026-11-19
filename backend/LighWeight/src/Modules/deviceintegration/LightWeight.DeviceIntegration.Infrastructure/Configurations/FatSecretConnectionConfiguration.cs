using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LightWeight.DeviceIntegration.Infrastructure.Configurations;

internal sealed class FatSecretConnectionConfiguration(TokenProtector tokenProtector) : IEntityTypeConfiguration<FatSecretConnection>
{
    public void Configure(EntityTypeBuilder<FatSecretConnection> builder)
    {
        builder.ToTable("deviceintegration_FatSecretConnections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.HasIndex(c => c.UserId).IsUnique().HasDatabaseName("Ux_FatSecretConnections_UserId");
        builder.Ignore(c => c.DomainEvents);
        builder.Ignore(c => c.IsLinked);

        // The tokens and secrets are stored encrypted. Nulls never reach the converter
        var encrypted = new ValueConverter<string?, string?>(v => tokenProtector.Protect(v!), v => tokenProtector.Unprotect(v!));
        builder.Property(c => c.AccessToken).HasConversion(encrypted);
        builder.Property(c => c.AccessTokenSecret).HasConversion(encrypted);
        builder.Property(c => c.RequestToken).HasConversion(encrypted);
        builder.Property(c => c.RequestTokenSecret).HasConversion(encrypted);
    }
}
