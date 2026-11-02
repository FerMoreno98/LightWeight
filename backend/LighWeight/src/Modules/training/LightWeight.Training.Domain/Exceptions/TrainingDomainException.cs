using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Domain.Exceptions;

public abstract class TrainingDomainException : BusinessException
{
    protected TrainingDomainException(string message) : base(message)
    {
        
    }
}