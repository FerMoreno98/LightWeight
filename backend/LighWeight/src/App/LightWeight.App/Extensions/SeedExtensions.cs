using LightWeight.Training.Infrastructure.Persistence;
using LightWeight.Training.Infrastructure.Persistence.Seed;

namespace LightWeight.App.Extensions;

public static class SeedExtensions
{
    public static async Task SeedTrainingDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingDbContext>();

        await ExerciseSeeder.SeedAsync(dbContext);
    }
}
