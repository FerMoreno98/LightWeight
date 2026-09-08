namespace LightWeight.Training.Application.Exceptions;

public sealed class TemplateSessionNotFoundApplicationException : TrainingApplicationException
{
    public TemplateSessionNotFoundApplicationException() : base("Session not found")
    {
        
    }
}