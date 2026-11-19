using LightWeight.Nutrition.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Nutrition.Domain.ValueObjects;

/// <summary>What the user ate in a day: energy (kcal) and macronutrients (g)</summary>
/// <remarks>
/// It is the total that the food diary of the user gives, so the calories are not checked against
/// the macros (4/4/9 kcal per gram): alcohol and the rounding of each food make them differ.
/// An instance is always valid: every value is zero or more.
/// </remarks>
public sealed record DailyIntake : ValueObject
{
    /// <summary>Used by EF Core to materialize the complex property</summary>
    private DailyIntake() { }

    /// <summary>Energy in kcal</summary>
    public decimal Calories { get; private init; }
    /// <summary>Protein in g</summary>
    public decimal Protein { get; private init; }
    /// <summary>Carbohydrate in g</summary>
    public decimal Carbohydrate { get; private init; }
    /// <summary>Fat in g</summary>
    public decimal Fat { get; private init; }

    public static DailyIntake Create(decimal calories, decimal protein, decimal carbohydrate, decimal fat)
    {
        EnsureNotNegative(calories, nameof(Calories));
        EnsureNotNegative(protein, nameof(Protein));
        EnsureNotNegative(carbohydrate, nameof(Carbohydrate));
        EnsureNotNegative(fat, nameof(Fat));
        return new DailyIntake { Calories = calories, Protein = protein, Carbohydrate = carbohydrate, Fat = fat };
    }

    private static void EnsureNotNegative(decimal value, string nutrient)
    {
        if (value < 0)
            throw new NegativeIntakeDomainException(nutrient);
    }
}
