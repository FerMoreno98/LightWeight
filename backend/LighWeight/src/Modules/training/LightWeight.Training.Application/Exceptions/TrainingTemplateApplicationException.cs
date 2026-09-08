namespace LightWeight.Training.Application.Exceptions;

public sealed class TrainingTemplateNotFoundApplicationException : TrainingApplicationException
{
    public TrainingTemplateNotFoundApplicationException() : base("The trainingTemplate do not correspond to any template")
    {
        
    }
}