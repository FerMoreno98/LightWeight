using LightWeight.BodyMetrics.Application.Commands.SkinFolds;

namespace LightWeight.BodyMetrics.Api.DTOs;

/// <summary>Skinfolds in mm (missing ones are null) and when they were taken (UTC)</summary>
public sealed record SkinFoldsRequest
(
    DateTime MeasuredAt,
    decimal? Abdominal,
    decimal? Suprailiac,
    decimal? Tricipital,
    decimal? Subscapular,
    decimal? Thigh,
    decimal? Calf
)
{
    public SkinFoldValues ToValues() => new
    (
        Abdominal,
        Suprailiac,
        Tricipital,
        Subscapular,
        Thigh,
        Calf
    );
}
