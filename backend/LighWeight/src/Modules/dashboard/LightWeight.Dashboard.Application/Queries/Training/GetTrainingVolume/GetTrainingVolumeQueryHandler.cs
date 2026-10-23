using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetTrainingVolume;

public sealed class GetTrainingVolumeQueryHandler : IQueryHandler<GetTrainingVolumeQuery, List<WeeklyVolumePoint>>
{
    private readonly ITrainingProjectionStore _store;

    public GetTrainingVolumeQueryHandler(ITrainingProjectionStore store)
        => _store = store;

    public async Task<List<WeeklyVolumePoint>> HandleAsync(GetTrainingVolumeQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        var sessions = await _store.GetSessionsAsync(query.UserId, ct);
        var sets = await _store.GetSetsAsync(query.UserId, ct: ct);
        return WeeklyVolume.Calculate(sessions, sets);
    }
}
