using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.Perimeters.GetPerimetersHistory;

/// <summary>Measurements of perimeters (cm) of the user, from the most recent</summary>
public sealed record GetPerimetersHistoryQuery(Guid UserId) : IQuery<List<PerimetersResponse>>;

public sealed record PerimetersResponse
(
    Guid Id,
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
