using LightWeight.BodyMetrics.Domain.Enum;
using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.Aggregates;

/// <summary>Skinfolds of a user taken at a moment, in mm</summary>
public sealed class SkinFolds : AggregateRoot<Guid>
{
    // EF Core cannot bind complex properties to constructor parameters, so Measures is set after construction
    private SkinFolds(Guid Id, Guid userId, DateTime measuredAt) : base(Id)
    {
        UserId = userId;
        MeasuredAt = measuredAt;
        Measures = null!;
    }

    public Guid UserId{get; private set;}
    /// <summary>The skinfolds taken (at least one)</summary>
    public SkinFoldMeasures Measures {get; private set;}
    /// <summary>When the skinfolds were taken (UTC)</summary>
    public DateTime MeasuredAt{get; private set;}

    /// <summary>Registers the skinfolds of a user</summary>
    /// <param name="userId">Owner of the measurement</param>
    /// <param name="measures">Skinfolds taken; they are already validated</param>
    /// <param name="measuredAt">When they were taken (UTC); it cannot be in the future</param>
    /// <param name="now">Current time (UTC)</param>
    public static SkinFolds Create(Guid userId, SkinFoldMeasures measures, DateTime measuredAt, DateTime now)
    {
        MeasurementRules.EnsureUser(userId);
        MeasurementRules.EnsureNotInTheFuture(measuredAt, now);
        var skinFolds = new SkinFolds(Guid.CreateVersion7(), userId, measuredAt) { Measures = measures };
        skinFolds.RaiseDomainEvent(new SkinFoldsRegisteredDomainEvent(skinFolds.Id, userId, measures, measuredAt));
        return skinFolds;
    }

    /// <summary>Replaces the skinfolds and/or the date of the measurement</summary>
    public void Correct(SkinFoldMeasures measures, DateTime measuredAt, DateTime now)
    {
        MeasurementRules.EnsureNotInTheFuture(measuredAt, now);
        Measures = measures;
        MeasuredAt = measuredAt;
        RaiseDomainEvent(new SkinFoldsCorrectedDomainEvent(Id, UserId, measures, measuredAt));
    }

    /// <summary>Records that the measurement is deleted. The repository removes it when saving</summary>
    public void Delete()
    {
        RaiseDomainEvent(new SkinFoldsDeletedDomainEvent(Id, UserId, MeasuredAt));
    }

    /// <summary>
    /// Estimated body fat percentage from the sum of the six skinfolds (linear formula by sex).
    /// Null unless all six skinfolds were measured
    /// </summary>
    public decimal? CalculateEstimatedFatPercentage(Sex sex)
    {
        if (Measures.Summatory is not decimal summatory) return null;
        return sex switch
        {
            Sex.Male => 0.1051m * summatory + 2.585m,
            Sex.Female => 0.1548m * summatory + 3.580m,
            _ => null
        };
    }
}
