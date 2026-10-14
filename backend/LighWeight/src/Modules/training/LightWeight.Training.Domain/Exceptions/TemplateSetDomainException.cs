namespace LightWeight.Training.Domain.Exceptions;

public sealed class ReptitionRangeLessThanZeroDomainException : TrainingDomainException
{
    public ReptitionRangeLessThanZeroDomainException() : base("Min or max Repetition Ranges can't be less than zero")
    {
        
    }
}
public sealed class SetNotFoundDomainException : TrainingDomainException
{
    public SetNotFoundDomainException() : base("Set not found")
    {
        
    }
}
public sealed class SuperSetNeedsTwoExercisesDomainException : TrainingDomainException
{
    public SuperSetNeedsTwoExercisesDomainException() : base("A superset needs at least two different exercises")
    {

    }
}
public sealed class SuperSetNotFoundDomainException : TrainingDomainException
{
    public SuperSetNotFoundDomainException() : base("The superset does not belong to this session")
    {

    }
}
