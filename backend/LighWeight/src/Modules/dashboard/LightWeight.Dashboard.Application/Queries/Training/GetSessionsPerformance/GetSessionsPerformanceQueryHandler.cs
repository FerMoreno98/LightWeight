using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;

public sealed class GetSessionsPerformanceQueryHandler : IQueryHandler<GetSessionsPerformanceQuery, List<SessionPerformanceResponse>>
{
    private readonly ITrainingProjectionStore _store;

    public GetSessionsPerformanceQueryHandler(ITrainingProjectionStore store)
        => _store = store;

    public async Task<List<SessionPerformanceResponse>> HandleAsync(GetSessionsPerformanceQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        var sessions = await _store.GetSessionsAsync(query.UserId, ct);
        var sets = await _store.GetSetsAsync(query.UserId, ct: ct);
        return SessionPerformance.Calculate(sessions, ExerciseHistory.Build(sessions, sets))
            .Select(p => new SessionPerformanceResponse
            (
                p.Session.SessionId,
                p.Session.Name,
                p.Session.StartedAt,
                p.Performance,
                p.ComparedExercises,
                p.Session.Motivation,
                p.Session.Sleep,
                p.Session.Doms,
                p.Excluded.ToList()
            ))
            .ToList();
    }
}
