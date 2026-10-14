using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.SkinFolds.GetSkinFoldsHistory;

/// <summary>Measurements of skinfolds (mm) of the user, from the most recent</summary>
public sealed record GetSkinFoldsHistoryQuery(Guid UserId) : IQuery<List<SkinFoldsResponse>>;

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
