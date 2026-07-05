namespace LightWeight.Training.Domain.Exceptions;

public abstract class TrainingDomainException : Exception
{
    protected TrainingDomainException(string message) : base(message)
    {
        
    }
}