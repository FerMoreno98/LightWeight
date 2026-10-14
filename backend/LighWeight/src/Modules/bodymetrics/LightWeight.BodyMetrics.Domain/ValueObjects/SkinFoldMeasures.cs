using LightWeight.BodyMetrics.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.ValueObjects;

/// <summary>
/// Skinfolds taken together in one measurement, in mm. Any of them can be missing, but at least one is needed.
/// An instance is always valid: the rules are checked when it is created
/// </summary>
public sealed record SkinFoldMeasures : ValueObject
{
    /// <summary>Largest skinfold accepted, in mm (catches typing errors)</summary>
    public const decimal MaxSkinFold = 100m;

    public decimal? Abdominal { get; init; }
    public decimal? Suprailiac { get; init; }
    public decimal? Tricipital { get; init; }
    public decimal? Subscapular { get; init; }
    public decimal? Thigh { get; init; }
    public decimal? Calf { get; init; }

    /// <summary>Sum of the six skinfolds; null unless all of them were measured</summary>
    public decimal? Summatory =>
        Abdominal.HasValue && Suprailiac.HasValue && Tricipital.HasValue && Subscapular.HasValue && Thigh.HasValue && Calf.HasValue
            ? Abdominal + Suprailiac + Tricipital + Subscapular + Thigh + Calf
            : null;

    private SkinFoldMeasures() { }

    /// <summary>Creates the skinfolds of a measurement</summary>
    /// <exception cref="MeasureOutOfRangeDomainException">A skinfold is not greater than 0 or exceeds the maximum</exception>
    /// <exception cref="NoSkinFoldMeasuredDomainException">Every skinfold is missing</exception>
    public static SkinFoldMeasures Create
    (
        decimal? abdominal = null,
        decimal? suprailiac = null,
        decimal? tricipital = null,
        decimal? subscapular = null,
        decimal? thigh = null,
        decimal? calf = null
    )
    {
        var measures = new SkinFoldMeasures
        {
            Abdominal = abdominal,
            Suprailiac = suprailiac,
            Tricipital = tricipital,
            Subscapular = subscapular,
            Thigh = thigh,
            Calf = calf,
        };
        (string Name, decimal? Value)[] values =
        [
            (nameof(Abdominal), abdominal),
            (nameof(Suprailiac), suprailiac),
            (nameof(Tricipital), tricipital),
            (nameof(Subscapular), subscapular),
            (nameof(Thigh), thigh),
            (nameof(Calf), calf),
        ];
        foreach (var (name, value) in values)
            MeasurementRules.EnsureInRange(value, name, 0, MaxSkinFold);
        if (values.All(v => v.Value is null))
            throw new NoSkinFoldMeasuredDomainException();
        return measures;
    }
}
