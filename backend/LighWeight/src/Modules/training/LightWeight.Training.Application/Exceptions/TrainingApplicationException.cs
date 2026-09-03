namespace LightWeight.Training.Application.Exceptions;

public abstract class TrainingApplicationException : Exception
{
    protected TrainingApplicationException(string message) : base(message)
    {
        
    }
}