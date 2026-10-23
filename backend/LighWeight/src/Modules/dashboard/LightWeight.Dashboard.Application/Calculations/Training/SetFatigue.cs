namespace LightWeight.Dashboard.Application.Calculations.Training;

/// <param name="FirstToSecond">Performance lost from the first to the second set, in % (negative if it improved)</param>
/// <param name="FirstToLast">Performance lost from the first to the last set, in %</param>
public sealed record FatiguePoint(DateTime PerformedAt, RepZone Zone, decimal FirstToSecond, decimal FirstToLast);

/// <summary>
/// Performance lost between the sets of an exercise in a session, comparing their estimated 1RM:
/// <c>(1RM first − 1RM other) / 1RM first × 100</c>. Only the sets that can be estimated count, in their order
/// </summary>
public static class SetFatigue
{
    /// <returns>Null with less than two sets that can be estimated</returns>
    public static FatiguePoint? Of(ExerciseInSession exercise)
    {
        var estimates = exercise.ValidEstimates;
        if (estimates.Count < 2) return null;

        decimal first = estimates[0].Value;
        return new FatiguePoint
        (
            exercise.Session.StartedAt,
            exercise.Zone,
            Lost(first, estimates[1].Value),
            Lost(first, estimates[^1].Value)
        );
    }

    private static decimal Lost(decimal first, decimal other)
        => -ExerciseHistory.PercentChange(first, other);
}
