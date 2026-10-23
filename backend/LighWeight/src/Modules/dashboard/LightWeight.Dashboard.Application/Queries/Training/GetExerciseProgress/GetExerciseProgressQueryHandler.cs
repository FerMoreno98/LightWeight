using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetExerciseProgress;

public sealed class GetExerciseProgressQueryHandler : IQueryHandler<GetExerciseProgressQuery, ExerciseProgressResponse?>
{
    /// <summary>Sessions shown in the table of the detail</summary>
    public const int RecentSessionsCount = 10;

    private readonly ITrainingProjectionStore _store;

    public GetExerciseProgressQueryHandler(ITrainingProjectionStore store)
        => _store = store;

    public async Task<ExerciseProgressResponse?> HandleAsync(GetExerciseProgressQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty || query.ExerciseId == Guid.Empty)
            throw new ArgumentException("The user and the exercise are required", nameof(query));

        var sets = await _store.GetSetsAsync(query.UserId, query.ExerciseId, ct);
        if (sets.Count == 0) return null;
        var sessions = await _store.GetSessionsAsync(query.UserId, ct);
        var history = ExerciseHistory.Build(sessions, sets);
        if (history.Count == 0) return null;

        return new ExerciseProgressResponse
        (
            query.ExerciseId,
            history[^1].ExerciseName,
            history
                .Where(e => e.Best is not null || e.BestWeight is not null)
                .Select(e => new ProgressionPoint
                (
                    e.Session.StartedAt,
                    e.Session.Name,
                    e.Zone.ToString(),
                    e.Best?.Value,
                    e.BestWeight,
                    e.Best?.LowPrecision ?? false,
                    e.Session.Motivation,
                    e.Session.Sleep,
                    e.Session.Doms
                ))
                .ToList(),
            history
                .Select(SetFatigue.Of)
                .Where(f => f is not null)
                .Select(f => new FatigueResponse(f!.PerformedAt, f.Zone.ToString(), f.FirstToSecond, f.FirstToLast))
                .ToList(),
            history
                .AsEnumerable()
                .Reverse()
                .Take(RecentSessionsCount)
                .Select(e => new RecentSession
                (
                    e.Session.StartedAt,
                    e.Session.Name,
                    e.Zone.ToString(),
                    e.Sets.Select((s, i) => new RecentSet(i + 1, s.Weight, s.Repetitions, s.Rpe, s.IsBodyWeight, s.Technique)).ToList()
                ))
                .ToList()
        );
    }
}
