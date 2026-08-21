using LightWeight.bodymetrics.Domain.Enum;
using LightWeight.bodymetrics.Domain.Events;
using LightWeight.bodymetrics.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.bodymetrics.Domain.Aggregates;

public sealed class SkinFolds : AggregateRoot<Guid>
{
    private SkinFolds
    (
        Guid Id,
        Guid userId,
        decimal? abdominal, 
        decimal? suprailiac, 
        decimal? tricipital, 
        decimal? subscapular, 
        decimal? thigh, 
        decimal? calf,
        DateTime measuredAt
    ) : base(Id)
    {
        UserId = userId;
        Abdominal = abdominal;
        Suprailiac = suprailiac;
        Tricipital = tricipital;
        Subscapular = subscapular;
        Thigh = thigh;
        Calf = calf;
        MeasuredAt = measuredAt;
    }
    public Guid UserId{get; private set;}
    public decimal? Abdominal {get;private set;}
    public decimal? Suprailiac {get;private set;}
    public decimal? Tricipital {get;private set;}
    public decimal? Subscapular {get;private set;}
    public decimal? Thigh {get;private set;}
    public decimal? Calf {get;private set;}
    public decimal? SkinFoldSummatory =>
    Abdominal.HasValue && Suprailiac.HasValue && Tricipital.HasValue && Subscapular.HasValue && Thigh.HasValue && Calf.HasValue
    ? Abdominal+Suprailiac+Tricipital+Subscapular+Thigh+Calf 
    : null;
    public DateTime MeasuredAt{get; private set;}

    public static SkinFolds Create
    (
        Guid userId,
        decimal? abdominal, 
        decimal? suprailiac, 
        decimal? tricipital, 
        decimal? subscapular, 
        decimal? thigh, 
        decimal? calf,
        DateTime measuredAt
    )
    {
        ValidateMeasurement(abdominal,nameof(abdominal));
        ValidateMeasurement(suprailiac,nameof(suprailiac));
        ValidateMeasurement(tricipital,nameof(tricipital));
        ValidateMeasurement(subscapular,nameof(subscapular));
        ValidateMeasurement(thigh,nameof(thigh));
        ValidateMeasurement(calf,nameof(calf));

        if
        (
            abdominal is null
            && suprailiac is null
            && tricipital is null
            && subscapular is null
            && thigh is null
            && calf is null
        )
            throw new EverySkinFoldNullException();
        
        SkinFolds skinFolds = new SkinFolds
        (
            Guid.CreateVersion7(),
            userId,
            abdominal, 
            suprailiac, 
            tricipital, 
            subscapular, 
            thigh, 
            calf,
            measuredAt 
        );
        skinFolds.RaiseDomainEvent(new RegisteredSkinFoldsDomainEvent(skinFolds.Id,skinFolds.UserId,skinFolds.MeasuredAt));
        return skinFolds;
    }

    private static void ValidateMeasurement(decimal? value,string skinfold)
    {
        if (value.HasValue && value.Value <= 0)
            throw new NegativeSkinFoldException(skinfold);
    }

    public decimal? CalculateEstimatedFatPercentage(Sex sex)
    {
        if (!SkinFoldSummatory.HasValue) return null;
    
        return sex switch
        {
            Sex.Male => 0.1051m * SkinFoldSummatory.Value + 2.585m,
            Sex.Female =>  0.1548m * SkinFoldSummatory.Value + 3.580m,
            _ => null
        };

    }
}