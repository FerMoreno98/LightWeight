using LightWeight.Dashboard.Application.Projections.Training;

namespace LightWeight.Dashboard.Application.Calculations.Training;

/// <param name="StartedAt">Date of the first session of the week, where the week is drawn</param>
/// <param name="SetsByMuscle">Sets per muscle group (enum name of training, e.g. "Chest")</param>
public sealed record WeeklyVolumePoint(Guid MicrocycleId, int WeekNumber, DateTime StartedAt, IReadOnlyDictionary<string, int> SetsByMuscle);

/// <summary>
/// Sets per muscle group and training week (microcycle, as the user plans it, not the calendar week).
/// Each set adds 1 to every muscle group it aimed at: the ones of its planned set, or, for an extra set,
/// the ones of the exercise performed
/// </summary>
public static class WeeklyVolume
{
    public static List<WeeklyVolumePoint> Calculate(IReadOnlyCollection<TrainingSessionProjection> sessions, IReadOnlyCollection<TrainingSetProjection> sets)
    {
        var setsBySession = sets.ToLookup(s => s.SessionId);
        return sessions
            .GroupBy(s => s.MicrocycleId)
            .Select(week =>
            {
                var volume = new Dictionary<string, int>();
                foreach (TrainingSetProjection set in week.SelectMany(session => setsBySession[session.SessionId]))
                    foreach (string muscle in MusclesOf(set))
                        volume[muscle] = volume.GetValueOrDefault(muscle) + 1;
                return new WeeklyVolumePoint(week.Key, week.First().WeekNumber, week.Min(s => s.StartedAt), volume);
            })
            .OrderBy(w => w.StartedAt)
            .ToList();
    }

    /// <summary>The planned muscle groups if the set was planned; the ones of the exercise for an extra set</summary>
    public static IReadOnlyCollection<string> MusclesOf(TrainingSetProjection set)
        => (set.PlannedMuscleGroups ?? set.ExerciseMuscleGroups).Distinct().ToList();
}
