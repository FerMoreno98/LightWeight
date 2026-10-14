using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.Weights.GetWeightHistory;

/// <summary>Measurements of body weight (kg) of the user, from the most recent</summary>
public sealed record GetWeightHistoryQuery(Guid UserId) : IQuery<List<WeightResponse>>;

/// <param name="Weight">Body weight in kg</param>
public sealed record WeightResponse
(
    Guid Id,
    DateTime MeasuredAt,
    decimal Weight
);
