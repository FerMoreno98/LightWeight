using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Queries.SkinFolds.GetSkinFoldsHistory;

public sealed class GetSkinFoldsHistoryQueryHandler : IQueryHandler<GetSkinFoldsHistoryQuery, List<SkinFoldsResponse>>
{
    private readonly ISkinFoldsRepository _repository;

    public GetSkinFoldsHistoryQueryHandler(ISkinFoldsRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SkinFoldsResponse>> HandleAsync(GetSkinFoldsHistoryQuery query, CancellationToken ct = default)
    {
        // The repository returns them from the most recent
        var measurements = await _repository.GetLatestOfAUserAsync(query.UserId, GetSkinFoldsHistoryQuery.Count);
        return measurements
            .Select(m => new SkinFoldsResponse(m.Id, m.MeasuredAt, m.Measures.Abdominal, m.Measures.Suprailiac, m.Measures.Tricipital, m.Measures.Subscapular, m.Measures.Thigh, m.Measures.Calf, m.Measures.Summatory))
            .ToList();
    }
}
