using LightWeight.BodyMetrics.Domain.Exceptions;

namespace LightWeight.BodyMetrics.Domain.ValueObjects;

/// <summary>Rules shared by every kind of measurement</summary>
internal static class MeasurementRules
{
    /// <summary>Margin for the difference between the client clock and the server clock</summary>
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    /// <summary>The value, when present, must be greater than min and at most max</summary>
    public static void EnsureInRange(decimal? value, string measure, decimal min, decimal max)
    {
        if (value.HasValue && (value.Value <= min || value.Value > max))
            throw new MeasureOutOfRangeDomainException(measure, min, max);
    }

    /// <summary>A measurement cannot be taken after now (allowing a small clock difference)</summary>
    public static void EnsureNotInTheFuture(DateTime measuredAt, DateTime now)
    {
        if (measuredAt > now + ClockTolerance)
            throw new MeasuredInTheFutureDomainException();
    }

    public static void EnsureUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new UserIdEmptyDomainException();
    }
}
