using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.Exceptions;
using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.Aggregates;

/// <summary>Body weight of a user at a moment, in kg</summary>
public sealed class WeightMeasurement : AggregateRoot<Guid>
{
    /// <summary>Lightest weight accepted, in kg (catches typing errors)</summary>
    public const decimal MinWeight = 20m;
    /// <summary>Heaviest weight accepted, in kg (catches typing errors)</summary>
    public const decimal MaxWeight = 400m;

    private WeightMeasurement
    (
        Guid Id,
        Guid userId,
        decimal weight,
        DateTime measuredAt
    ) : base(Id)
    {
        UserId = userId;
        Weight = weight;
        MeasuredAt = measuredAt;
    }

    public Guid UserId{get; private set;}
    /// <summary>Body weight in kg</summary>
    public decimal Weight{get; private set;}
    /// <summary>When the user weighed themselves (UTC)</summary>
    public DateTime MeasuredAt{get;private set;}

    /// <summary>Registers the weight of a user</summary>
    /// <param name="userId">Owner of the measurement</param>
    /// <param name="weight">Weight in kg, between <see cref="MinWeight"/> and <see cref="MaxWeight"/></param>
    /// <param name="measuredAt">When it was measured (UTC); it cannot be in the future</param>
    /// <param name="now">Current time (UTC)</param>
    public static WeightMeasurement Create(Guid userId, decimal weight, DateTime measuredAt, DateTime now)
    {
        MeasurementRules.EnsureUser(userId);
        EnsureValid(weight, measuredAt, now);
        var measurement = new WeightMeasurement(Guid.CreateVersion7(), userId, weight, measuredAt);
        measurement.RaiseDomainEvent(new WeightRegisteredDomainEvent(measurement.Id, userId, weight, measuredAt));
        return measurement;
    }

    /// <summary>Corrects the weight and/or the date of the measurement</summary>
    public void Correct(decimal weight, DateTime measuredAt, DateTime now)
    {
        EnsureValid(weight, measuredAt, now);
        Weight = weight;
        MeasuredAt = measuredAt;
        RaiseDomainEvent(new WeightCorrectedDomainEvent(Id, UserId, weight, measuredAt));
    }

    /// <summary>Records that the measurement is deleted. The repository removes it when saving</summary>
    public void Delete()
    {
        RaiseDomainEvent(new WeightDeletedDomainEvent(Id, UserId, MeasuredAt));
    }

    private static void EnsureValid(decimal weight, DateTime measuredAt, DateTime now)
    {
        if (weight < MinWeight || weight > MaxWeight)
            throw new MeasureOutOfRangeDomainException(nameof(Weight), MinWeight, MaxWeight);
        MeasurementRules.EnsureNotInTheFuture(measuredAt, now);
    }
}
