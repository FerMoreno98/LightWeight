using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.BodyMetrics.GetSkinFoldsEvolution;

/// <summary>Every measurement of skinfolds of the user, from the oldest</summary>
public sealed record GetSkinFoldsEvolutionQuery(Guid UserId) : IQuery<List<SkinFoldsPoint>>;

/// <summary>Skinfolds in mm; the ones not measured are null, and Summatory only exists with the six</summary>
public sealed record SkinFoldsPoint
(
    DateTime MeasuredAt,
    decimal? Abdominal,
    decimal? Suprailiac,
    decimal? Tricipital,
    decimal? Subscapular,
    decimal? Thigh,
    decimal? Calf,
    decimal? Summatory
);
