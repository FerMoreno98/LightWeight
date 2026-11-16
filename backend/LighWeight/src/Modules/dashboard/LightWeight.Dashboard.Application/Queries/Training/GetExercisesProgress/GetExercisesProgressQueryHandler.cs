using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetExercisesProgress;

public sealed class GetExercisesProgressQueryHandler : IQueryHandler<GetExercisesProgressQuery, List<ExerciseProgressSummary>>
{
    private readonly ITrainingProjectionStore _store;

    public GetExercisesProgressQueryHandler(ITrainingProjectionStore store)
        => _store = store;

    public async Task<List<ExerciseProgressSummary>> HandleAsync(GetExercisesProgressQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        var sessions = await _store.GetSessionsAsync(query.UserId, ct);
        var sets = await _store.GetSetsAsync(query.UserId, ct: ct);
        return ExerciseHistory.Build(sessions, sets)
            .GroupBy(e => e.ExerciseId)
            .Where(exercise => query.ExercisesMacrocycleId is not { } macrocycleId
                || exercise.Any(e => e.Session.MacrocycleId == macrocycleId))
            .Select(exercise =>
            {
                ExerciseInSession last = exercise.Last();
                var marks = ExerciseHistory.InMacrocycle(exercise, query.MarksMacrocycleId);
                return new ExerciseProgressSummary
                (
                    exercise.Key,
                    last.ExerciseName,
                    last.Session.StartedAt,
                    marks.Count,
                    marks.LastOrDefault(e => e.Best is not null)?.Best!.Value,
                    ExerciseHistory.Trend(marks)
                );
            })
            .OrderByDescending(e => e.LastPerformedAt)
            .ToList();
    }
}
