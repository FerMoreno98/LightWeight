namespace LightWeight.BodyMetrics.Application.Utils;

internal static class UtcDate
{
    /// <summary>
    /// The measurement dates are stored with time zone, so they must be UTC. A date sent without zone
    /// ("2026-10-14T08:00:00") is taken as UTC, and a local one is converted
    /// </summary>
    public static DateTime Normalize(DateTime date) => date.Kind switch
    {
        DateTimeKind.Utc => date,
        DateTimeKind.Local => date.ToUniversalTime(),
        _ => DateTime.SpecifyKind(date, DateTimeKind.Utc),
    };
}
