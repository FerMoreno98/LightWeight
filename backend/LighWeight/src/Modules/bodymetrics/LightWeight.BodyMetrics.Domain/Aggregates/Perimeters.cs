using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.Aggregates;

/// <summary>Perimeters of a user taken at a moment, in cm</summary>
public sealed class Perimeters : AggregateRoot<Guid>
{
    // EF Core cannot bind complex properties to constructor parameters, so Measures is set after construction
    private Perimeters(Guid Id, Guid userId, DateTime measuredAt) : base(Id)
    {
        UserId = userId;
        MeasuredAt = measuredAt;
        Measures = null!;
    }

    public Guid UserId {get; private set;}
    /// <summary>The perimeters taken (at least one)</summary>
    public PerimeterMeasures Measures {get; private set;}
    /// <summary>When the perimeters were taken (UTC)</summary>
    public DateTime MeasuredAt{get; private set;}

    /// <summary>Registers the perimeters of a user</summary>
    /// <param name="userId">Owner of the measurement</param>
    /// <param name="measures">Perimeters taken; they are already validated</param>
    /// <param name="measuredAt">When they were taken (UTC); it cannot be in the future</param>
    /// <param name="now">Current time (UTC)</param>
    public static Perimeters Create(Guid userId, PerimeterMeasures measures, DateTime measuredAt, DateTime now)
    {
        MeasurementRules.EnsureUser(userId);
        MeasurementRules.EnsureNotInTheFuture(measuredAt, now);
        var perimeters = new Perimeters(Guid.CreateVersion7(), userId, measuredAt) { Measures = measures };
        perimeters.RaiseDomainEvent(new PerimetersRegisteredDomainEvent(perimeters.Id, userId, measures, measuredAt));
        return perimeters;
    }

    /// <summary>Replaces the perimeters and/or the date of the measurement</summary>
    public void Correct(PerimeterMeasures measures, DateTime measuredAt, DateTime now)
    {
        MeasurementRules.EnsureNotInTheFuture(measuredAt, now);
        Measures = measures;
        MeasuredAt = measuredAt;
        RaiseDomainEvent(new PerimetersCorrectedDomainEvent(Id, UserId, measures, measuredAt));
    }

    /// <summary>Records that the measurement is deleted. The repository removes it when saving</summary>
    public void Delete()
    {
        RaiseDomainEvent(new PerimetersDeletedDomainEvent(Id, UserId, MeasuredAt));
    }
}
