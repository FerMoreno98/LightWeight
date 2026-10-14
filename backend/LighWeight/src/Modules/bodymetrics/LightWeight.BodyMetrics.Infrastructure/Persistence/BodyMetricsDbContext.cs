using LightWeight.BodyMetrics.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.BodyMetrics.Infrastructure.Persistence;

public class BodyMetricsDbContext(DbContextOptions<BodyMetricsDbContext> options) : DbContext(options)
{
    public DbSet<WeightMeasurement> WeightMeasurements => Set<WeightMeasurement>();
    public DbSet<Perimeters> Perimeters => Set<Perimeters>();
    public DbSet<SkinFolds> SkinFolds => Set<SkinFolds>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bodymetrics");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BodyMetricsDbContext).Assembly);
    }
}
