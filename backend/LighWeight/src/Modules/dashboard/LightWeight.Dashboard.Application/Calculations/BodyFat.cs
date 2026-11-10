namespace LightWeight.Dashboard.Application.Calculations;

/// <summary>
/// Estimated fat percentage from the summatory of the six skinfolds (abdominal, suprailiac, tricipital,
/// subscapular, thigh, calf), with the linear formula by sex of Yuhasz. It is the same one of
/// <c>SkinFolds.CalculateEstimatedFatPercentage</c> in bodymetrics: the dashboard cannot reference that domain,
/// so if the formula changes there it has to change here too
/// </summary>
public static class BodyFat
{
    /// <summary>
    /// Null without the six skinfolds (no summatory) or without the sex of the user. Rounded to 2 decimals
    /// </summary>
    /// <param name="sex">Male or Female, as UserProfile publishes it (case-insensitive)</param>
    public static decimal? EstimatePercentage(decimal? summatory, string? sex)
    {
        if (summatory is not decimal sum) return null;

        decimal? percentage = sex?.ToLowerInvariant() switch
        {
            "male" => 0.1051m * sum + 2.585m,
            "female" => 0.1548m * sum + 3.580m,
            _ => null
        };
        return percentage is decimal p ? Math.Round(p, 2) : null;
    }
}
