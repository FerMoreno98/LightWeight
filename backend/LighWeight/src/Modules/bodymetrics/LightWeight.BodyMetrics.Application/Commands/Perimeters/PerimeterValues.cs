using LightWeight.BodyMetrics.Domain.ValueObjects;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters;

/// <summary>Perimeters (cm) sent by the client. Missing ones are null; the domain checks there is at least one</summary>
public sealed record PerimeterValues
(
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
    /// <summary>Builds the domain value object, which validates the measures</summary>
    public PerimeterMeasures ToMeasures() => PerimeterMeasures.Create
    (
            neck: Neck,
            rightArmRelaxed: RightArmRelaxed,
            rightArmTensioned: RightArmTensioned,
            leftArmRelaxed: LeftArmRelaxed,
            leftArmTensioned: LeftArmTensioned,
            chest: Chest,
            shoulder: Shoulder,
            waist: Waist,
            hip: Hip,
            abs: Abs,
            rightThigh: RightThigh,
            leftThigh: LeftThigh,
            rightCalf: RightCalf,
            leftCalf: LeftCalf
    );
}
