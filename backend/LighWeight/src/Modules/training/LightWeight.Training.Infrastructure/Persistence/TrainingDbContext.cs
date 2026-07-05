namespace LightWeight.Training.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;





public class TrainingDbContext(DbContextOptions<TrainingDbContext> options) : DbContext(options)
{

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("training");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrainingDbContext).Assembly);
    }
}