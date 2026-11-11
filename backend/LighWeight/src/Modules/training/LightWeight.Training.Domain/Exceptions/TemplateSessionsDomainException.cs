using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Domain.Exceptions;

public sealed class SessionNameEmptyDomainException : TrainingDomainException
{
    public SessionNameEmptyDomainException() : base("The name cannot be null or empty")
    {
        
    }
}
public sealed class SessionNotFoundDomainException : TrainingDomainException, INotFoundException
{
    public SessionNotFoundDomainException() : base("This session does not correspond to any sessiontemplate")
    {
        
    }
}
/// <summary>A new order of the sets of a session must list every active set of the session exactly once</summary>
public sealed class InvalidSetOrderDomainException : TrainingDomainException
{
    public InvalidSetOrderDomainException() : base("The new order must contain every set of the session exactly once")
    {

    }
}
