using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Infrastructure.Persistence.Seed;

/// <summary>An exercise of the catalog, as written in catalogo-ejercicios.csv</summary>
public sealed record CatalogExercise
(
    string Name,
    Laterality Laterality,
    List<MuscleGroups> PrimaryMuscleGroups,
    List<MuscleGroups> SecondaryMuscleGroups
);

/// <summary>
/// Reads the exercise catalog: a CSV separated by ";" with the columns grupo_principal; ejercicio; secundarios;
/// lateralidad; nota. Muscle groups and laterality are written in Spanish, several groups separated by ","
/// </summary>
public static class ExerciseCatalog
{
    public const string ResourceName = "LightWeight.Training.Infrastructure.Persistence.Seed.catalogo-ejercicios.csv";

    private static readonly Dictionary<string, MuscleGroups> MuscleGroupsByLabel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pecho"] = MuscleGroups.Chest,
        ["Espalda"] = MuscleGroups.Back,
        ["Trapecio"] = MuscleGroups.Traps,
        ["Deltoides anterior"] = MuscleGroups.FrontDelts,
        ["Deltoides lateral"] = MuscleGroups.SideDelts,
        ["Deltoides posterior"] = MuscleGroups.RearDelts,
        ["Tríceps"] = MuscleGroups.Triceps,
        ["Bíceps"] = MuscleGroups.Biceps,
        ["Antebrazo"] = MuscleGroups.Forearms,
        ["Abdomen"] = MuscleGroups.Abs,
        ["Glúteos"] = MuscleGroups.Glutes,
        ["Cuádriceps"] = MuscleGroups.Quads,
        ["Isquios"] = MuscleGroups.Hamstring,
        ["Gemelos"] = MuscleGroups.Calves,
        ["Aductores"] = MuscleGroups.Adductors,
    };

    private static readonly Dictionary<string, Laterality> LateralityByLabel = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bilateral"] = Laterality.Bilateral,
        ["unilateral"] = Laterality.Unilateral,
        ["ambos"] = Laterality.Both,
    };

    /// <summary>Reads the catalog embedded in this assembly</summary>
    public static List<CatalogExercise> Load()
    {
        using Stream stream = typeof(ExerciseCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found");
        using var reader = new StreamReader(stream);
        return Parse(reader);
    }

    /// <summary>Parses the catalog, failing with the line number on any unknown value or repeated name</summary>
    public static List<CatalogExercise> Parse(TextReader reader)
    {
        var exercises = new List<CatalogExercise>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        reader.ReadLine(); // header
        int lineNumber = 1;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] columns = line.Split(';');
            if (columns.Length < 4)
                throw Error(lineNumber, "expected at least 4 columns");

            string name = columns[1].Trim();
            if (name.Length == 0) throw Error(lineNumber, "empty exercise name");
            if (!names.Add(name)) throw Error(lineNumber, $"repeated exercise \"{name}\"");

            List<MuscleGroups> primary = ParseGroups(columns[0], lineNumber);
            if (primary.Count == 0) throw Error(lineNumber, "no primary muscle group");
            List<MuscleGroups> secondary = ParseGroups(columns[2], lineNumber);
            if (secondary.Intersect(primary).Any())
                throw Error(lineNumber, "a muscle group is both primary and secondary");
            if (!LateralityByLabel.TryGetValue(columns[3].Trim(), out Laterality laterality))
                throw Error(lineNumber, $"unknown laterality \"{columns[3]}\"");

            exercises.Add(new CatalogExercise(name, laterality, primary, secondary));
        }
        return exercises;
    }

    private static List<MuscleGroups> ParseGroups(string column, int lineNumber)
        => column.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(label => MuscleGroupsByLabel.TryGetValue(label, out MuscleGroups group)
                ? group
                : throw Error(lineNumber, $"unknown muscle group \"{label}\""))
            .Distinct()
            .ToList();

    private static FormatException Error(int lineNumber, string message)
        => new($"catalogo-ejercicios.csv, line {lineNumber}: {message}");
}
