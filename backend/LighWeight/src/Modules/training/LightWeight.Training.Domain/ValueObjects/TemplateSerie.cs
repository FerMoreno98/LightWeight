using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.ValueObjects;

public sealed record TemplateSerie : ValueObject
{
    public TemplateSerie
    (
        string repetitionRange, 
        bool isTopSet
    )
    {
        RepetitionRange = repetitionRange;
        IsTopSet = isTopSet;
    }

    public string RepetitionRange {get; private set;}
    public bool IsTopSet {get; private set;}
}