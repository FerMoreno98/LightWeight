using LightWeight.Dashboard.Application.Calculations;
using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.BodyMetrics.GetWeightEvolution;

public sealed class GetWeightEvolutionQueryHandler : IQueryHandler<GetWeightEvolutionQuery, WeightEvolutionResponse>
{
    private readonly IBodyMetricsProjectionStore _store;

    public GetWeightEvolutionQueryHandler(IBodyMetricsProjectionStore store)
        => _store = store;

    public async Task<WeightEvolutionResponse> HandleAsync(GetWeightEvolutionQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        var weights = await _store.GetWeightsAsync(query.UserId, ct);
        return new WeightEvolutionResponse
        (
            weights.Select(w => new WeightPoint(w.MeasuredAt, w.Weight)).ToList(),
            WeeklyAverages.Calculate(weights.Select(w => (w.MeasuredAt, w.Weight)))
        );
    }
}
