using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.SkinFolds.GetSkinFoldsHistory;

/// <summary>The latest measurements of skinfolds (mm) of the user, from the most recent</summary>
public sealed record GetSkinFoldsHistoryQuery(Guid UserId) : IQuery<List<SkinFoldsResponse>>
{
    /// <summary>
    /// Measurements returned: the history is to check and correct the recent ones. The whole evolution
    /// is shown by the dashboard, which receives every measurement through the integration events
    /// </summary>
    public const int Count = 10;
}

/// <param name="Summatory">Sum of the six skinfolds, null unless all of them were measured</param>
public sealed record SkinFoldsResponse
(
    Guid Id,
    DateTime MeasuredAt,
    decimal? Abdominal,
    decimal? Suprailiac,
    decimal? Tricipital,
    decimal? Subscapular,
    decimal? Thigh,
    decimal? Calf,
    decimal? Summatory
);
