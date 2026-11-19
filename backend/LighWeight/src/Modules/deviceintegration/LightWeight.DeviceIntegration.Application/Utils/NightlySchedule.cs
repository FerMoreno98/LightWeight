using LightWeight.shared.Types;

namespace LightWeight.DeviceIntegration.Application.Utils;

/// <summary>When the nightly synchronization runs: every day at 03:30 in Europe/Madrid</summary>
public static class NightlySchedule
{
    /// <summary>03:30 always exists and is never repeated in Madrid: the clock changes at 02:00/03:00</summary>
    public static readonly TimeOnly RunAt = new(3, 30);

    /// <summary>The next run after <paramref name="utcNow"/>, in UTC</summary>
    public static DateTime NextRunUtc(DateTime utcNow)
    {
        DateOnly today = UserCalendar.Today(utcNow);
        DateTime run = ToUtc(today);
        return run > utcNow ? run : ToUtc(today.AddDays(1));
    }

    private static DateTime ToUtc(DateOnly day)
        => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(RunAt, DateTimeKind.Unspecified), UserCalendar.TimeZone);
}
