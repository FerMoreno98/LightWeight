using LightWeight.Dashboard.Application.Calculations;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.BodyMetrics.GetWeightEvolution;

/// <summary>Every body weight of the user, from the oldest, and the average of each week</summary>
public sealed record GetWeightEvolutionQuery(Guid UserId) : IQuery<WeightEvolutionResponse>;

public sealed record WeightEvolutionResponse
(
    List<WeightPoint> Points,
    List<WeeklyAverage> WeeklyAverages
);

/// <param name="Weight">Body weight in kg</param>
public sealed record WeightPoint(DateTime MeasuredAt, decimal Weight);
