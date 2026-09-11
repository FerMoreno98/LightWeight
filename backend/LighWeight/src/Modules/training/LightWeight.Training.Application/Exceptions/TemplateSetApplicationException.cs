namespace LightWeight.Training.Application.Exceptions;

public sealed class TemplateSetNotFoundApplicationException : TrainingApplicationException
{
    public TemplateSetNotFoundApplicationException() : base("Set not found")
    {
        
    }
}