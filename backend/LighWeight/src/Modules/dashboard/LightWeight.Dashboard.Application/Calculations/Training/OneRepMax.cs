namespace LightWeight.Dashboard.Application.Calculations.Training;

/// <param name="Value">Estimated one repetition maximum, in kg (1 decimal)</param>
/// <param name="LowPrecision">Whether it comes from a long set (reps + RIR over 15), where the formula is less precise</param>
public sealed record EstimatedOneRepMax(decimal Value, bool LowPrecision);

/// <summary>
/// Estimated 1RM of a set with Epley's formula, counting the repetitions left in reserve (RIR = 10 - RPE):
/// <c>weight × (1 + (reps + RIR) / 30)</c>. E.g. 100 kg × 10 @ RPE 8 → 100 × (1 + 12/30) = 140 kg
/// </summary>
public static class OneRepMax
{
    /// <summary>Above this (reps + RIR) the estimation is flagged as less precise</summary>
    public const int PreciseUpTo = 15;
    /// <summary>Above this (reps + RIR) no estimation is made</summary>
    public const int EstimatedUpTo = 30;

    /// <returns>
    /// Null when the set cannot be compared by its load: body weight sets (the weight is not the real load), sets with a
    /// technique (drop set, cluster, myo-rep perform worse by design), without load or without repetitions, or too long
    /// </returns>
    public static EstimatedOneRepMax? Estimate(decimal weight, int repetitions, decimal rpe, bool isBodyWeight, string? technique)
    {
        if (isBodyWeight || technique is not null || weight <= 0 || repetitions <= 0)
            return null;

        decimal repetitionsToFailure = repetitions + (10 - rpe);
        if (repetitionsToFailure > EstimatedUpTo)
            return null;

        decimal value = weight * (1 + repetitionsToFailure / 30m);
        return new EstimatedOneRepMax(Math.Round(value, 1, MidpointRounding.AwayFromZero), repetitionsToFailure > PreciseUpTo);
    }
}
