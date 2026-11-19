using LightWeight.Nutrition.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Nutrition.Infrastructure.Persistence;

public class NutritionDbContext(DbContextOptions<NutritionDbContext> options) : DbContext(options)
{
    public DbSet<DiaryDay> DiaryDays => Set<DiaryDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("nutrition");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NutritionDbContext).Assembly);
    }
}
