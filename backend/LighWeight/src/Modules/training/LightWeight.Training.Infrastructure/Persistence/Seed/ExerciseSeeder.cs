using LightWeight.Training.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Seed;

/// <summary>
/// Keeps the exercises in sync with the catalog (catalogo-ejercicios.csv), matching them by name: new ones are
/// added and existing ones get the catalog's laterality and muscle groups. Exercises missing from the catalog are
/// kept, since sets may use them
/// </summary>
public static class ExerciseSeeder
{
    public static async Task SeedAsync(TrainingDbContext dbContext)
    {
        List<CatalogExercise> catalog = ExerciseCatalog.Load();
        Dictionary<string, Exercise> existing = (await dbContext.Exercises.ToListAsync())
            .ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);

        foreach (CatalogExercise item in catalog)
        {
            if (existing.TryGetValue(item.Name, out Exercise? exercise))
                exercise.UpdateClassification(item.Laterality, item.PrimaryMuscleGroups, item.SecondaryMuscleGroups);
            else
                dbContext.Exercises.Add(Exercise.Create(item.Name, item.Laterality, item.PrimaryMuscleGroups, item.SecondaryMuscleGroups));
        }

        await dbContext.SaveChangesAsync();
    }
}
