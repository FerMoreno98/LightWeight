using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public abstract class TrainingApplicationException : BusinessException
{
    protected TrainingApplicationException(string message) : base(message)
    {
        
    }
}