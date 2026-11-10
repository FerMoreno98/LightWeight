using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.Dashboard.Application.Projections.UserProfile;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Dashboard.Infrastructure.Persistence;

public class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<WeightProjection> BodyWeights => Set<WeightProjection>();
    public DbSet<PerimetersProjection> BodyPerimeters => Set<PerimetersProjection>();
    public DbSet<SkinFoldsProjection> BodySkinFolds => Set<SkinFoldsProjection>();
    public DbSet<TrainingSessionProjection> TrainingSessions => Set<TrainingSessionProjection>();
    public DbSet<TrainingSetProjection> TrainingSets => Set<TrainingSetProjection>();
    public DbSet<UserProfileProjection> UserProfiles => Set<UserProfileProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dashboard");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DashboardDbContext).Assembly);
    }
}
