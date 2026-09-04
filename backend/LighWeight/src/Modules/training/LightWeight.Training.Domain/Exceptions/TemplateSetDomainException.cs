namespace LightWeight.Training.Domain.Exceptions;

public sealed class ReptitionRangeLessThanZeroDomainException : TrainingDomainException
{
    public ReptitionRangeLessThanZeroDomainException() : base("Min or max Repetition Ranges can't be less than zero")
    {
        
    }
}