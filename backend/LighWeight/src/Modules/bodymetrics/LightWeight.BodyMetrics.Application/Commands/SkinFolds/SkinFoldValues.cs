using LightWeight.BodyMetrics.Domain.ValueObjects;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds;

/// <summary>Skinfolds (mm) sent by the client. Missing ones are null; the domain checks there is at least one</summary>
public sealed record SkinFoldValues
(
    decimal? Abdominal,
    decimal? Suprailiac,
    decimal? Tricipital,
    decimal? Subscapular,
    decimal? Thigh,
    decimal? Calf
)
{
    /// <summary>Builds the domain value object, which validates the measures</summary>
    public SkinFoldMeasures ToMeasures() => SkinFoldMeasures.Create
    (
            abdominal: Abdominal,
            suprailiac: Suprailiac,
            tricipital: Tricipital,
            subscapular: Subscapular,
            thigh: Thigh,
            calf: Calf
    );
}
