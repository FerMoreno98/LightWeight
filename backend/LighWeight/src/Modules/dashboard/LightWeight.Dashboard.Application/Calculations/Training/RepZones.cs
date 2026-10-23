namespace LightWeight.Dashboard.Application.Calculations.Training;

/// <summary>Kind of effort, by repetitions. Fatigue and progression are only compared inside the same zone</summary>
public enum RepZone
{
    Strength,
    Hypertrophy,
    Endurance
}

public static class RepZones
{
    /// <summary>Up to this many repetitions: strength</summary>
    public const int StrengthUpTo = 6;
    /// <summary>Up to this many repetitions: hypertrophy; more: endurance</summary>
    public const int HypertrophyUpTo = 15;

    /// <summary>
    /// Zone of an exercise in a session, from its first set. The planned range is the intention, so it is used when
    /// there is one (its middle: 8-10 → 9); an extra set uses the repetitions performed
    /// </summary>
    public static RepZone Of(int? plannedMinReps, int? plannedMaxReps, int performedRepetitions)
    {
        decimal repetitions = plannedMinReps is int min && plannedMaxReps is int max
            ? (min + max) / 2m
            : performedRepetitions;

        if (repetitions <= StrengthUpTo) return RepZone.Strength;
        if (repetitions <= HypertrophyUpTo) return RepZone.Hypertrophy;
        return RepZone.Endurance;
    }
}
