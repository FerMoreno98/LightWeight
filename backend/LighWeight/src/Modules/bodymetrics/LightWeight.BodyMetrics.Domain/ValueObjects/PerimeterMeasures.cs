using LightWeight.BodyMetrics.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.ValueObjects;

/// <summary>
/// Perimeters taken together in one measurement, in cm. Any of them can be missing, but at least one is needed.
/// An instance is always valid: the rules are checked when it is created
/// </summary>
public sealed record PerimeterMeasures : ValueObject
{
    /// <summary>Largest perimeter accepted, in cm (catches typing errors)</summary>
    public const decimal MaxPerimeter = 300m;

    public decimal? Neck { get; init; }
    public decimal? RightArmRelaxed { get; init; }
    public decimal? RightArmTensioned { get; init; }
    public decimal? LeftArmRelaxed { get; init; }
    public decimal? LeftArmTensioned { get; init; }
    public decimal? Chest { get; init; }
    public decimal? Shoulder { get; init; }
    public decimal? Waist { get; init; }
    public decimal? Hip { get; init; }
    public decimal? Abs { get; init; }
    public decimal? RightThigh { get; init; }
    public decimal? LeftThigh { get; init; }
    public decimal? RightCalf { get; init; }
    public decimal? LeftCalf { get; init; }

    private PerimeterMeasures() { }

    /// <summary>Creates the perimeters of a measurement</summary>
    /// <exception cref="MeasureOutOfRangeDomainException">A perimeter is not greater than 0 or exceeds the maximum</exception>
    /// <exception cref="NoPerimeterMeasuredDomainException">Every perimeter is missing</exception>
    public static PerimeterMeasures Create
    (
        decimal? neck = null,
        decimal? rightArmRelaxed = null,
        decimal? rightArmTensioned = null,
        decimal? leftArmRelaxed = null,
        decimal? leftArmTensioned = null,
        decimal? chest = null,
        decimal? shoulder = null,
        decimal? waist = null,
        decimal? hip = null,
        decimal? abs = null,
        decimal? rightThigh = null,
        decimal? leftThigh = null,
        decimal? rightCalf = null,
        decimal? leftCalf = null
    )
    {
        var measures = new PerimeterMeasures
        {
            Neck = neck,
            RightArmRelaxed = rightArmRelaxed,
            RightArmTensioned = rightArmTensioned,
            LeftArmRelaxed = leftArmRelaxed,
            LeftArmTensioned = leftArmTensioned,
            Chest = chest,
            Shoulder = shoulder,
            Waist = waist,
            Hip = hip,
            Abs = abs,
            RightThigh = rightThigh,
            LeftThigh = leftThigh,
            RightCalf = rightCalf,
            LeftCalf = leftCalf,
        };
        var values = measures.Values();
        foreach (var (name, value) in values)
            MeasurementRules.EnsureInRange(value, name, 0, MaxPerimeter);
        if (values.All(v => v.Value is null))
            throw new NoPerimeterMeasuredDomainException();
        return measures;
    }

    /// <summary>Every perimeter with its name, in a fixed order</summary>
    private IReadOnlyList<(string Name, decimal? Value)> Values() =>
    [
        (nameof(Neck), Neck),
        (nameof(RightArmRelaxed), RightArmRelaxed),
        (nameof(RightArmTensioned), RightArmTensioned),
        (nameof(LeftArmRelaxed), LeftArmRelaxed),
        (nameof(LeftArmTensioned), LeftArmTensioned),
        (nameof(Chest), Chest),
        (nameof(Shoulder), Shoulder),
        (nameof(Waist), Waist),
        (nameof(Hip), Hip),
        (nameof(Abs), Abs),
        (nameof(RightThigh), RightThigh),
        (nameof(LeftThigh), LeftThigh),
        (nameof(RightCalf), RightCalf),
        (nameof(LeftCalf), LeftCalf),
    ];
}
