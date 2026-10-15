namespace LightWeight.Dashboard.Application.Calculations;

/// <param name="WeekStart">Monday 00:00 (UTC) of the week</param>
/// <param name="Average">Average of the values of the week, rounded to 2 decimals</param>
/// <param name="Count">Values the average is made of</param>
public sealed record WeeklyAverage(DateTime WeekStart, decimal Average, int Count);

/// <summary>
/// Groups dated values by ISO week (Monday to Sunday) and averages them. Smooths the daily fluctuations
/// of the body weight. Pure calculation over data that is already valid: it lives here and not in a Domain
/// layer because it protects no invariant, it only reads
/// </summary>
public static class WeeklyAverages
{
    /// <summary>
    /// Weeks are cut in UTC. Weigh-ins are usually taken in the morning, so in Spain (UTC+1/+2) no measurement
    /// changes week; if that ever matters, the time zone of the user would be passed here
    /// </summary>
    public static List<WeeklyAverage> Calculate(IEnumerable<(DateTime At, decimal Value)> values)
        => values
            .GroupBy(v => WeekStartOf(v.At))
            .OrderBy(week => week.Key)
            .Select(week => new WeeklyAverage(
                week.Key,
                Math.Round(week.Average(v => v.Value), 2, MidpointRounding.AwayFromZero),
                week.Count()))
            .ToList();

    /// <summary>Monday 00:00 UTC of the week of the date</summary>
    public static DateTime WeekStartOf(DateTime at)
    {
        // Dates come from timestamptz columns (UTC); one without zone is taken as UTC, never as local time
        DateTime utc = at.Kind == DateTimeKind.Local ? at.ToUniversalTime() : at;
        DateTime day = DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
        // DayOfWeek starts on Sunday (0): Monday -> 0 days back, Sunday -> 6
        int daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-daysSinceMonday);
    }
}
