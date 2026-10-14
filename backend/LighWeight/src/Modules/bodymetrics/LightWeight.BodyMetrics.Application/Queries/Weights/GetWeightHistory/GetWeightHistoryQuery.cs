using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.Weights.GetWeightHistory;

/// <summary>The latest measurements of body weight (kg) of the user, from the most recent</summary>
public sealed record GetWeightHistoryQuery(Guid UserId) : IQuery<List<WeightResponse>>
{
    /// <summary>
    /// Measurements returned: the history is to check and correct the recent ones. The whole evolution
    /// is shown by the dashboard, which receives every measurement through the integration events
    /// </summary>
    public const int Count = 10;
}

/// <param name="Weight">Body weight in kg</param>
public sealed record WeightResponse
(
    Guid Id,
    DateTime MeasuredAt,
    decimal Weight
);
