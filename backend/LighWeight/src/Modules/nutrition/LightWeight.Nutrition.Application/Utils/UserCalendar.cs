namespace LightWeight.Nutrition.Application.Utils;

/// <summary>Calendar days of the user. The time zone is fixed to Europe/Madrid until users have their own</summary>
public static class UserCalendar
{
    public static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    /// <summary>The calendar day of the user at a UTC moment</summary>
    public static DateOnly Today(DateTime utcNow)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), TimeZone));
}
