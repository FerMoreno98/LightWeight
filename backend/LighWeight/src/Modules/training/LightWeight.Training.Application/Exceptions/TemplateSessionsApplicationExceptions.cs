using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public sealed class TemplateSessionNotFoundApplicationException : TrainingApplicationException, INotFoundException
{
    public TemplateSessionNotFoundApplicationException() : base("Session not found")
    {
        
    }
}