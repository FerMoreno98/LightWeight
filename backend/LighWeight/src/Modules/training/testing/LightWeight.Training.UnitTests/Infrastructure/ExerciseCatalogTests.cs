using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Infrastructure.Persistence.Seed;

namespace LightWeight.Training.UnitTests.Infrastructure;

public class ExerciseCatalogTests
{
    private const string Header = "grupo_principal;ejercicio;secundarios;lateralidad;nota";

    [Fact]
    public void Load_TheEmbeddedCatalog_IsValidAndCoversEveryMuscleGroup()
    {
        List<CatalogExercise> catalog = ExerciseCatalog.Load();

        Assert.True(catalog.Count >= 339);
        Assert.All(System.Enum.GetValues<MuscleGroups>(),
            group => Assert.Contains(catalog, e => e.PrimaryMuscleGroups.Contains(group)));
    }

    [Fact]
    public void Parse_ARow_ReadsGroupsAndLaterality()
    {
        var csv = $"{Header}\nPecho;Press banca;Tríceps, Deltoides anterior;ambos;\n";

        CatalogExercise exercise = Assert.Single(ExerciseCatalog.Parse(new StringReader(csv)));

        Assert.Equal("Press banca", exercise.Name);
        Assert.Equal(Laterality.Both, exercise.Laterality);
        Assert.Equal([MuscleGroups.Chest], exercise.PrimaryMuscleGroups);
        Assert.Equal([MuscleGroups.Triceps, MuscleGroups.FrontDelts], exercise.SecondaryMuscleGroups);
    }

    [Theory]
    [InlineData("Pecho;Press banca;Hombro;bilateral;")]
    [InlineData("Pecho;Press banca;;a veces;")]
    [InlineData("Pecho;Press banca;Pecho;bilateral;")]
    [InlineData(";Press banca;;bilateral;")]
    [InlineData("Pecho;Press banca;;bilateral;\nPecho;press banca;;bilateral;")]
    public void Parse_AnInvalidRow_FailsWithTheLineNumber(string rows)
    {
        var exception = Assert.Throws<FormatException>(() => ExerciseCatalog.Parse(new StringReader($"{Header}\n{rows}")));

        Assert.Contains("line", exception.Message);
    }
}
