using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.Perimeters.GetPerimetersHistory;

public sealed class GetPerimetersHistoryQueryHandler : IQueryHandler<GetPerimetersHistoryQuery, List<PerimetersResponse>>
{
    private readonly IPerimetersRepository _repository;

    public GetPerimetersHistoryQueryHandler(IPerimetersRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PerimetersResponse>> HandleAsync(GetPerimetersHistoryQuery query, CancellationToken ct = default)
    {
        // The repository returns them from the most recent
        var measurements = await _repository.GetAllOfAUserAsync(query.UserId);
        return measurements
            .Select(m => new PerimetersResponse(m.Id, m.MeasuredAt, m.Measures.Neck, m.Measures.RightArmRelaxed, m.Measures.RightArmTensioned, m.Measures.LeftArmRelaxed, m.Measures.LeftArmTensioned, m.Measures.Chest, m.Measures.Shoulder, m.Measures.Waist, m.Measures.Hip, m.Measures.Abs, m.Measures.RightThigh, m.Measures.LeftThigh, m.Measures.RightCalf, m.Measures.LeftCalf))
            .ToList();
    }
}
