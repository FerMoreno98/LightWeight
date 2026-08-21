using LightWeight.bodymetrics.Domain.Events;
using LightWeight.bodymetrics.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;
namespace LightWeight.bodymetrics.Domain.Aggregates;

public sealed class Perimeters : AggregateRoot<Guid>
{
    private Perimeters
    (
        Guid Id,
        Guid userId,
        decimal? neck,
        decimal? rightArmRelaxed,
        decimal? rightArmTensioned, 
        decimal? leftArmRelaxed, 
        decimal? leftArmTensioned, 
        decimal? chest, 
        decimal? shoulder, 
        decimal? waist, 
        decimal? hip, 
        decimal? abs, 
        decimal? rightThigh, 
        decimal? leftThigh, 
        decimal? rightCalf, 
        decimal? leftCalf,
        DateTime measuredAt
    ) : base (Id)
    {
        UserId = userId;
        Neck = neck;
        RightArmRelaxed = rightArmRelaxed;
        RightArmTensioned = rightArmTensioned;
        LeftArmRelaxed = leftArmRelaxed;
        LeftArmTensioned = leftArmTensioned;
        Chest = chest;
        Shoulder = shoulder;
        Waist = waist;
        Hip = hip;
        Abs = abs;
        RightThigh = rightThigh;
        LeftThigh = leftThigh;
        RightCalf = rightCalf;
        LeftCalf = leftCalf;
        MeasuredAt = measuredAt;
    }
    public Guid UserId {get; private set;}
    public decimal? Neck{get; private set;}
    public decimal? RightArmRelaxed {get;private set;}
    public decimal? RightArmTensioned {get;private set;}
    public decimal? LeftArmRelaxed {get;private set;}
    public decimal? LeftArmTensioned {get;private set;}
    public decimal? Chest{get;private set;}
    public decimal? Shoulder {get;private set;}
    public decimal? Waist {get;private set;}
    public decimal? Hip {get;private set;}
    public decimal? Abs {get;private set;}
    public decimal? RightThigh {get;private set;}
    public decimal? LeftThigh {get;private set;}
    public decimal? RightCalf {get;private set;}
    public decimal? LeftCalf {get;private set;}
    public DateTime MeasuredAt{get; private set;}

    public static Perimeters Create
    (
        Guid userId,
        decimal? neck,
        decimal? rightArmRelaxed,
        decimal? rightArmTensioned, 
        decimal? leftArmRelaxed, 
        decimal? leftArmTensioned, 
        decimal? chest, 
        decimal? shoulder, 
        decimal? waist, 
        decimal? hip, 
        decimal? abs, 
        decimal? rightThigh, 
        decimal? leftThigh, 
        decimal? rightCalf, 
        decimal? leftCalf,
        DateTime measuredAt
    )
    {
        ValidateMeasurement(rightArmRelaxed,nameof(rightArmRelaxed));
        ValidateMeasurement(rightArmTensioned, nameof(rightArmTensioned));
        ValidateMeasurement(leftArmRelaxed, nameof(leftArmRelaxed));
        ValidateMeasurement(leftArmTensioned,nameof(leftArmTensioned));
        ValidateMeasurement(chest,nameof(chest));
        ValidateMeasurement(shoulder,nameof(shoulder));
        ValidateMeasurement(waist,nameof(waist));
        ValidateMeasurement(hip,nameof(hip));
        ValidateMeasurement(abs,nameof(abs));
        ValidateMeasurement(rightThigh,nameof(rightThigh));
        ValidateMeasurement(leftThigh,nameof(leftThigh));
        ValidateMeasurement(rightCalf,nameof(rightCalf));
        ValidateMeasurement(leftCalf,nameof(leftCalf));
        if
        (
            neck is null
            && rightArmRelaxed is null 
            && rightArmTensioned is null
            && leftArmRelaxed is null
            && leftArmTensioned is null
            && chest is null
            && shoulder is null
            && waist is null
            && hip is null
            && abs is null
            && rightThigh is null
            && leftThigh is null
            && rightCalf is null
            && leftCalf is null
        )
            throw new EveryPerimeterNullException();

        Perimeters perimeters = new Perimeters
        (
            Guid.CreateVersion7(), 
            userId,
            neck,
            rightArmRelaxed,
            rightArmTensioned, 
            leftArmRelaxed, 
            leftArmTensioned, 
            chest, 
            shoulder, 
            waist, 
            hip, 
            abs, 
            rightThigh, 
            leftThigh, 
            rightCalf, 
            leftCalf,
            measuredAt
        );
        perimeters.RaiseDomainEvent(new RegisteredPerimetersDomainEvent(perimeters.Id,perimeters.UserId,perimeters.MeasuredAt));
        return perimeters;
    }
    private static void ValidateMeasurement(decimal? value,string perimeter)
    {
        if (value.HasValue && value.Value <= 0)
            throw new NegativePerimeterException(perimeter);
    }

}