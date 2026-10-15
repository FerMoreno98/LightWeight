using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.BodyMetrics.GetPerimetersEvolution;

/// <summary>Every measurement of perimeters of the user, from the oldest</summary>
public sealed record GetPerimetersEvolutionQuery(Guid UserId) : IQuery<List<PerimetersPoint>>;

/// <summary>Perimeters in cm; the ones not measured that day are null</summary>
public sealed record PerimetersPoint
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
);
