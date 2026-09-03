namespace LightWeight.Training.Infrastructure.Persistence;

using LightWeight.Training.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;





public class TrainingDbContext(DbContextOptions<TrainingDbContext> options) : DbContext(options)
{
    public DbSet<Macrocycle> Macrocycles => Set<Macrocycle>();
    public DbSet<Mesocycle> Mesocycles => Set<Mesocycle>();
    public DbSet<TrainingTemplate> TrainingTemplates => Set<TrainingTemplate>();
    public DbSet<Exercise> Exercises => Set<Exercise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("training");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrainingDbContext).Assembly);
    }
}