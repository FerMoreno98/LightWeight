using LightWeight.BodyMetrics.Application.Commands.Perimeters;

namespace LightWeight.BodyMetrics.Api.DTOs;

/// <summary>Perimeters in cm (missing ones are null) and when they were taken (UTC)</summary>
public sealed record PerimetersRequest
(
    DateTime MeasuredAt,
    decimal? Neck,
    decimal? RightArmRelaxed,
    decimal? RightArmTensioned,
    decimal? LeftArmRelaxed,
    decimal? LeftArmTensioned,
    decimal? Chest,
    decimal? Shoulder,
    decimal? Waist,
    decimal? Hip,
    decimal? Abs,
    decimal? RightThigh,
    decimal? LeftThigh,
    decimal? RightCalf,
    decimal? LeftCalf
)
{
    public PerimeterValues ToValues() => new
    (
        Neck,
        RightArmRelaxed,
        RightArmTensioned,
        LeftArmRelaxed,
        LeftArmTensioned,
        Chest,
        Shoulder,
        Waist,
        Hip,
        Abs,
        RightThigh,
        LeftThigh,
        RightCalf,
        LeftCalf
    );
}
