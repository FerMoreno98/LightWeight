using LightWeight.shared.BuildingBlocks;

namespace LightWeight.bodymetrics.Domain.Aggregates;

public sealed class WeightMeasurments : AggregateRoot<Guid>
{
    private WeightMeasurments
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
    public decimal Weight{get; private set;}
    public DateTime MeasuredAt{get;private set;}

    // public static WeightMeasurments Create()
    // {
        
    // }
}