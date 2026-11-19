using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Infrastructure.Configurations;
using LightWeight.DeviceIntegration.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.DeviceIntegration.Infrastructure.Persistence;

internal sealed class DeviceIntegrationDbContext(DbContextOptions<DeviceIntegrationDbContext> options, TokenProtector tokenProtector)
    : DbContext(options)
{
    public DbSet<FatSecretConnection> FatSecretConnections => Set<FatSecretConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("deviceintegration");
        // Built by hand: the configuration needs the protector to encrypt the tokens
        modelBuilder.ApplyConfiguration(new FatSecretConnectionConfiguration(tokenProtector));
    }
}
