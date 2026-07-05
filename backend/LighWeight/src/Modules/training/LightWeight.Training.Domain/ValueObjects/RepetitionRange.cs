using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.ValueObjects;


public sealed record RepetitionRange : ValueObject
{
    private RepetitionRange
    (
        int max, 
        int min)
    {
        Max = max;
        Min = min;
    }

    /// <summary>Maximum repetitions in the range</summary>
    public int Max{get;init;}
    /// <summary>Minimum repetitions in the range</summary>
    public int Min {get;init;}
    /// <summary>Creates a repetition range, swapping values if max is lower than min</summary>
    /// <param name="max">Upper bound</param>
    /// <param name="min">Lower bound</param>
    public static RepetitionRange Create
    (
        int max,
        int min
    )
    {
        if (max < min)
        {
            int temp = max;
            max = min;
            min = temp;
        }
     return new RepetitionRange(max,min);    
    }
}