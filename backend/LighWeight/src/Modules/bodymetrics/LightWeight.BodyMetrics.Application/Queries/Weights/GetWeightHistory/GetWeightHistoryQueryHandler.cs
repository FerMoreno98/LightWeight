using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.Weights.GetWeightHistory;

public sealed class GetWeightHistoryQueryHandler : IQueryHandler<GetWeightHistoryQuery, List<WeightResponse>>
{
    private readonly IWeightMeasurementRepository _repository;

    public GetWeightHistoryQueryHandler(IWeightMeasurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<WeightResponse>> HandleAsync(GetWeightHistoryQuery query, CancellationToken ct = default)
    {
        // The repository returns them from the most recent
        var measurements = await _repository.GetLatestOfAUserAsync(query.UserId, GetWeightHistoryQuery.Count);
        return measurements
            .Select(m => new WeightResponse(m.Id, m.MeasuredAt, m.Weight))
            .ToList();
    }
}
