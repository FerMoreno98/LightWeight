namespace LightWeight.Training.Domain.Exceptions;

public sealed class SessionNameEmptyDomainException : TrainingDomainException
{
    public SessionNameEmptyDomainException() : base("The name cannot be null or empty")
    {
        
    }
}
public sealed class SessionNotFoundDomainException : TrainingDomainException
{
    public SessionNotFoundDomainException() : base("This session does not correspond to any sessiontemplate")
    {
        
    }
}