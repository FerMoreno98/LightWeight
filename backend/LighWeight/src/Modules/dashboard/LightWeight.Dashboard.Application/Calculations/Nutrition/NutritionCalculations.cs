using LightWeight.Dashboard.Application.Projections.Nutrition;

namespace LightWeight.Dashboard.Application.Calculations.Nutrition;

/// <summary>What the user ate around a session, to read next to its performance</summary>
/// <param name="PreviousDayCalories">kcal of the day before the session (null: no diary that day)</param>
/// <param name="PreviousDayCarbohydratePerKg">Carbohydrate of the day before, in g per kg of body weight (null: no diary or no weight)</param>
/// <param name="SameDayCalories">kcal of the day of the session</param>
/// <param name="ProteinPerKgLast7Days">Average protein of the 7 days before, g/kg; needs <see cref="NutritionCalculations.MinDaysForProteinAverage"/> days with diary</param>
public sealed record SessionNutrition
(
    decimal? PreviousDayCalories,
    decimal? PreviousDayCarbohydratePerKg,
    decimal? SameDayCalories,
    decimal? ProteinPerKgLast7Days
);

/// <param name="WeekStart">Monday of the week (calendar day of the user)</param>
/// <param name="Calories">Average kcal of the days with diary of the week</param>
/// <param name="DaysLogged">Days of the week with diary (1-7): a week with few days is less reliable</param>
/// <param name="ProteinPerKg">Average protein of those days per kg of body weight (null without any weight)</param>
public sealed record NutritionWeek(DateOnly WeekStart, decimal Calories, int DaysLogged, decimal? ProteinPerKg);

/// <summary>
/// Calculations over the diary days, done when reading (pure, like the training ones). The body weight for the g/kg
/// is the weekly average of the week of the day, or the closest earlier one; before the first weigh-in, the first
/// </summary>
public static class NutritionCalculations
{
    /// <summary>Days with diary needed in the 7 days before a session for its average protein</summary>
    public const int MinDaysForProteinAverage = 4;
    /// <summary>Days of the "last 4 weeks" average</summary>
    public const int RecentDays = 28;

    public static decimal? BodyWeightAt(IReadOnlyList<WeeklyAverage> weeklyWeights, DateOnly day)
    {
        if (weeklyWeights.Count == 0)
            return null;
        DateTime at = day.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        WeeklyAverage? latest = weeklyWeights.LastOrDefault(w => w.WeekStart <= at);
        return (latest ?? weeklyWeights[0]).Average;
    }

    /// <summary>Null when there is nothing of the diary around the session</summary>
    public static SessionNutrition? AroundSession(DateOnly sessionDay, IReadOnlyDictionary<DateOnly, NutritionDayProjection> days, decimal? bodyWeight)
    {
        days.TryGetValue(sessionDay.AddDays(-1), out NutritionDayProjection? previous);
        days.TryGetValue(sessionDay, out NutritionDayProjection? same);
        List<NutritionDayProjection> lastWeek = Enumerable.Range(1, 7)
            .Select(i => sessionDay.AddDays(-i))
            .Where(days.ContainsKey)
            .Select(d => days[d])
            .ToList();

        decimal? weight = bodyWeight is > 0 ? bodyWeight : null;
        decimal? protein = lastWeek.Count >= MinDaysForProteinAverage && weight is not null
            ? Round(lastWeek.Average(d => d.Protein) / weight.Value, 2)
            : null;
        decimal? carbohydrate = previous is not null && weight is not null ? Round(previous.Carbohydrate / weight.Value, 2) : null;

        if (previous is null && same is null && protein is null)
            return null;
        return new SessionNutrition(previous?.Calories, carbohydrate, same?.Calories, protein);
    }

    /// <summary>One entry per week with diary, from the oldest. Weeks are Monday to Sunday</summary>
    public static List<NutritionWeek> Weeks(IEnumerable<NutritionDayProjection> days, IReadOnlyList<WeeklyAverage> weeklyWeights)
        => days
            .GroupBy(d => WeekStartOf(d.Date))
            .OrderBy(w => w.Key)
            .Select(w =>
            {
                decimal? weight = BodyWeightAt(weeklyWeights, w.Key);
                return new NutritionWeek(
                    w.Key,
                    Round(w.Average(d => d.Calories), 0),
                    w.Count(),
                    weight is > 0 ? Round(w.Average(d => d.Protein) / weight.Value, 2) : null);
            })
            .ToList();

    /// <summary>Average kcal of every day with diary: the reference the weeks are compared with</summary>
    public static decimal? AverageCalories(IReadOnlyCollection<NutritionDayProjection> days)
        => days.Count == 0 ? null : Round(days.Average(d => d.Calories), 0);

    /// <summary>Average kcal of the days with diary of the last <see cref="RecentDays"/> days, today included</summary>
    public static decimal? RecentCalories(IEnumerable<NutritionDayProjection> days, DateOnly today)
    {
        DateOnly from = today.AddDays(-(RecentDays - 1));
        List<NutritionDayProjection> recent = days.Where(d => d.Date >= from && d.Date <= today).ToList();
        return recent.Count == 0 ? null : Round(recent.Average(d => d.Calories), 0);
    }

    public static DateOnly WeekStartOf(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    private static decimal Round(decimal value, int decimals) => Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}
