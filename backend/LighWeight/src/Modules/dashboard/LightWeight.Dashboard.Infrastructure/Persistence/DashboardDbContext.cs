using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Dashboard.Infrastructure.Persistence;

public class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<WeightProjection> BodyWeights => Set<WeightProjection>();
    public DbSet<PerimetersProjection> BodyPerimeters => Set<PerimetersProjection>();
    public DbSet<SkinFoldsProjection> BodySkinFolds => Set<SkinFoldsProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dashboard");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DashboardDbContext).Assembly);
    }
}
