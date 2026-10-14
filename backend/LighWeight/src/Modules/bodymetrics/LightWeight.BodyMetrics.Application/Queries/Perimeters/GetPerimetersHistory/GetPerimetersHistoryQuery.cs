using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.Perimeters.GetPerimetersHistory;

/// <summary>The latest measurements of perimeters (cm) of the user, from the most recent</summary>
public sealed record GetPerimetersHistoryQuery(Guid UserId) : IQuery<List<PerimetersResponse>>
{
    /// <summary>
    /// Measurements returned: the history is to check and correct the recent ones. The whole evolution
    /// is shown by the dashboard, which receives every measurement through the integration events
    /// </summary>
    public const int Count = 10;
}

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
