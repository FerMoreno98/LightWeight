namespace LightWeight.Training.Domain.Exceptions;

public sealed class SessionNameEmptyDomainException : TrainingDomainException
{
    public SessionNameEmptyDomainException() : base("The name cannot be null or empty")
    {
        
    }
}