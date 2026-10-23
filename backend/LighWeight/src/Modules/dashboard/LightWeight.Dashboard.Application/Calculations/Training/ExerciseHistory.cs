using LightWeight.Dashboard.Application.Projections.Training;

namespace LightWeight.Dashboard.Application.Calculations.Training;

/// <summary>
/// How an exercise was performed in one session: its sets in order, the zone of the effort and the estimated 1RM of
/// each set (null for the sets that cannot be estimated). The base of every per-exercise calculation
/// </summary>
public sealed record ExerciseInSession
(
    Guid ExerciseId,
    string ExerciseName,
    TrainingSessionProjection Session,
    RepZone Zone,
    IReadOnlyList<TrainingSetProjection> Sets,
    IReadOnlyList<EstimatedOneRepMax?> Estimates
)
{
    /// <summary>The highest estimated 1RM of the session</summary>
    public EstimatedOneRepMax? Best => Estimates.Where(e => e is not null).MaxBy(e => e!.Value);

    /// <summary>The estimated 1RM of the first set that can be estimated: the freshest one, without fatigue of the session</summary>
    public EstimatedOneRepMax? First => Estimates.FirstOrDefault(e => e is not null);

    /// <summary>The highest load moved (body weight sets excluded)</summary>
    public decimal? BestWeight => Sets.Where(s => !s.IsBodyWeight && s.Weight > 0).Select(s => (decimal?)s.Weight).Max();

    /// <summary>The estimates that exist, in the order of the sets</summary>
    public IReadOnlyList<EstimatedOneRepMax> ValidEstimates => Estimates.Where(e => e is not null).Select(e => e!).ToList();
}

public static class ExerciseHistory
{
    /// <summary>Every exercise of every session, ordered by the date of the session</summary>
    /// <param name="sessions">The sessions the sets belong to</param>
    /// <param name="sets">Sets of those sessions (all of them, or only the ones of an exercise)</param>
    public static List<ExerciseInSession> Build(IReadOnlyCollection<TrainingSessionProjection> sessions, IReadOnlyCollection<TrainingSetProjection> sets)
    {
        var sessionsById = sessions.ToDictionary(s => s.SessionId);
        return sets
            .Where(s => sessionsById.ContainsKey(s.SessionId))
            .GroupBy(s => new { s.SessionId, s.ExerciseId })
            .Select(group =>
            {
                var ordered = group.OrderBy(s => s.Order).ToList();
                TrainingSetProjection first = ordered[0];
                return new ExerciseInSession
                (
                    group.Key.ExerciseId,
                    first.ExerciseName,
                    sessionsById[group.Key.SessionId],
                    RepZones.Of(first.PlannedMinReps, first.PlannedMaxReps, first.Repetitions),
                    ordered,
                    ordered.Select(s => OneRepMax.Estimate(s.Weight, s.Repetitions, s.Rpe, s.IsBodyWeight, s.Technique)).ToList()
                );
            })
            .OrderBy(e => e.Session.StartedAt)
            .ThenBy(e => e.Sets[0].Order)
            .ToList();
    }

    /// <summary>
    /// Change (%) of the best estimated 1RM of the last session against the previous session in the same zone.
    /// Null when there is no previous comparable session
    /// </summary>
    /// <param name="history">The sessions of one exercise, from the oldest</param>
    public static decimal? Trend(IReadOnlyList<ExerciseInSession> history)
    {
        int lastIndex = history.ToList().FindLastIndex(e => e.Best is not null);
        if (lastIndex < 0) return null;
        ExerciseInSession last = history[lastIndex];
        ExerciseInSession? previous = history
            .Take(lastIndex)
            .LastOrDefault(e => e.Zone == last.Zone && e.Best is not null);
        return previous is null ? null : PercentChange(previous.Best!.Value, last.Best!.Value);
    }

    /// <summary>Change from one value to another, in % with 1 decimal</summary>
    public static decimal PercentChange(decimal from, decimal to)
        => Math.Round((to - from) / from * 100, 1, MidpointRounding.AwayFromZero);
}
