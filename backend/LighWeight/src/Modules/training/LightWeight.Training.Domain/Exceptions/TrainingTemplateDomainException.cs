namespace LightWeight.Training.Domain.Exceptions;

public sealed class NameEmptyDomainException : TrainingDomainException
{
    public NameEmptyDomainException() : base("The name cannot be null or empty")
    {
        
    }
}
public sealed class UserIdEmptyDomainException : TrainingDomainException
{
    public UserIdEmptyDomainException() : base("The UserId cannot be null or empty")
    {
        
    }
}
public sealed class InvalidVolumeLandmarkDomainException : TrainingDomainException
{
    public InvalidVolumeLandmarkDomainException() : base("the volumelandmark selected does not exists or is empty")
    {
        
    }
}
public sealed class InvalidTrainingDistributionDomainException : TrainingDomainException
{
    public  InvalidTrainingDistributionDomainException() : base("the training distribution selected does not exists or is empty")
    {
        
    }
}