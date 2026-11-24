using LightWeight.Dashboard.Application.Projections.Training;

namespace LightWeight.Dashboard.Application.Calculations.Training;

/// <param name="ExerciseName">Exercise whose change was left out of the average</param>
/// <param name="Change">Its change against the previous time, in %</param>
public sealed record ExcludedComparison(string ExerciseName, decimal Change);

/// <param name="Performance">Average change (%) of the session against the previous time of its exercises; null if none could be compared</param>
/// <param name="ComparedExercises">Exercises that took part in the average</param>
/// <param name="Excluded">Comparisons left out because their change is not plausible (see <see cref="SessionPerformance.MaxPlausibleChange"/>)</param>
public sealed record SessionPerformancePoint(TrainingSessionProjection Session, decimal? Performance, int ComparedExercises, IReadOnlyList<ExcludedComparison> Excluded);

/// <summary>
/// How a session went compared to the previous time: for each exercise, the estimated 1RM of its first set (the freshest
/// one, it shows how the user arrived that day) against the first set of the same time of the exercise in the previous
/// week, in the same zone: the 1st time of the week against the 1st time of the previous week, the 2nd against the 2nd.
/// The session's performance is the average of those changes. Comparing with the previous time, and not with an
/// absolute value, keeps the normal progression (more load every week) from hiding a bad day
/// </summary>
/// <remarks>
/// An exercise done twice in a week (Monday and Thursday) is never compared with the other day of the same week: the
/// recovery, its position in the session and the volume before it differ, and that difference would show up as a change
/// of performance every week. The time of the week is counted per microcycle and not per session of the template,
/// because changing the template of the next microcycle (base → accumulation) gives its sessions new ids
/// </remarks>
public static class SessionPerformance
{
    /// <summary>
    /// A change bigger than this (%, up or down) from one session to the next is not a real change of performance:
    /// it is almost always a registration error (10 kg instead of 100) or a weighted/assisted exercise registered with
    /// the added load only. It is left out of the average and reported, so the user can check it
    /// </summary>
    public const decimal MaxPlausibleChange = 50;

    /// <param name="sessions">Every session of the user</param>
    /// <param name="history">Every exercise of every session (see <see cref="ExerciseHistory.Build"/>)</param>
    /// <returns>One point per session, from the oldest; the sessions without comparable exercises have a null performance</returns>
    public static List<SessionPerformancePoint> Calculate(IReadOnlyCollection<TrainingSessionProjection> sessions, IReadOnlyList<ExerciseInSession> history)
    {
        var changesBySession = new Dictionary<Guid, List<decimal>>();
        var excludedBySession = new Dictionary<Guid, List<ExcludedComparison>>();
        // Times each exercise and zone has been done in each week so far
        var timesInWeek = new Dictionary<(Guid MicrocycleId, Guid ExerciseId, RepZone Zone), int>();
        // Last first-set estimate of each exercise, zone and time of the week seen so far, walking the history from the
        // oldest. A time of the week cannot repeat inside its week, so the reference is always of a previous week: the
        // last one that had that time (a deload or a week with less frequency is skipped)
        var previous = new Dictionary<(Guid ExerciseId, RepZone Zone, int Time), decimal>();

        foreach (ExerciseInSession exercise in history)
        {
            // Every time counts, also the ones that cannot be estimated: Thursday is still the 2nd time of the week
            // when Monday's first set was a drop set
            var week = (exercise.Session.MicrocycleId, exercise.ExerciseId, exercise.Zone);
            int time = timesInWeek[week] = timesInWeek.GetValueOrDefault(week) + 1;

            if (exercise.First is not EstimatedOneRepMax first) continue;
            var key = (exercise.ExerciseId, exercise.Zone, time);
            if (previous.TryGetValue(key, out decimal before))
            {
                decimal change = ExerciseHistory.PercentChange(before, first.Value);
                if (Math.Abs(change) > MaxPlausibleChange)
                    ListOf(excludedBySession, exercise.Session.SessionId).Add(new ExcludedComparison(exercise.ExerciseName, change));
                else
                    ListOf(changesBySession, exercise.Session.SessionId).Add(change);
            }
            // Even when excluded, the new value is the reference for the next time: if it was the error, the next
            // comparison will be excluded too; if the previous one was the error, the next one is already right
            previous[key] = first.Value;
        }

        return sessions
            .OrderBy(s => s.StartedAt)
            .Select(session =>
            {
                var excluded = excludedBySession.GetValueOrDefault(session.SessionId) ?? [];
                return changesBySession.TryGetValue(session.SessionId, out var changes)
                    ? new SessionPerformancePoint(session, Math.Round(changes.Average(), 1, MidpointRounding.AwayFromZero), changes.Count, excluded)
                    : new SessionPerformancePoint(session, null, 0, excluded);
            })
            .ToList();
    }

    private static List<T> ListOf<T>(Dictionary<Guid, List<T>> lists, Guid sessionId)
    {
        if (!lists.TryGetValue(sessionId, out var list))
            lists[sessionId] = list = new List<T>();
        return list;
    }
}
