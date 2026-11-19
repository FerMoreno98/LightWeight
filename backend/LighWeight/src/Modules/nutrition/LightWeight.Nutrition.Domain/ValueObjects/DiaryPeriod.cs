using LightWeight.Nutrition.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Nutrition.Domain.ValueObjects;

/// <summary>Days of the diary read in one synchronization, both included</summary>
public sealed record DiaryPeriod : ValueObject
{
    private DiaryPeriod(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    public DateOnly From { get; }
    public DateOnly To { get; }

    public static DiaryPeriod Create(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new InvalidDiaryPeriodDomainException(from, to);
        return new DiaryPeriod(from, to);
    }

    public bool Contains(DateOnly date) => date >= From && date <= To;
}
