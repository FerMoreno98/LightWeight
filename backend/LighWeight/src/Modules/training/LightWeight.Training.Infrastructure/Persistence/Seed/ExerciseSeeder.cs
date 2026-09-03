using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Seed;

public static class ExerciseSeeder
{
    public static async Task SeedAsync(TrainingDbContext dbContext)
    {
        if (await dbContext.Exercises.AnyAsync())
        {
            return;
        }

        var exercises = new[]
        {
            Exercise.Create("Press banca", true, [MuscleGroups.Chest, MuscleGroups.Triceps, MuscleGroups.Shoulder]),
            Exercise.Create("Sentadilla", true, [MuscleGroups.Quads, MuscleGroups.Glutes, MuscleGroups.Hamstring]),
            Exercise.Create("Peso muerto", true, [MuscleGroups.Back, MuscleGroups.Hamstring, MuscleGroups.Glutes]),
            Exercise.Create("Dominadas", true, [MuscleGroups.Back, MuscleGroups.Biceps]),
            Exercise.Create("Press militar", true, [MuscleGroups.Shoulder, MuscleGroups.Triceps]),
            Exercise.Create("Curl de bíceps con mancuerna", false, [MuscleGroups.Biceps]),
        };

        dbContext.Exercises.AddRange(exercises);
        await dbContext.SaveChangesAsync();
    }
}
