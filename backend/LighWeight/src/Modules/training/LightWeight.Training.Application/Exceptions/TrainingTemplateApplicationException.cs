using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public sealed class TrainingTemplateNotFoundApplicationException : TrainingApplicationException, INotFoundException
{
    public TrainingTemplateNotFoundApplicationException() : base("The trainingTemplate do not correspond to any template")
    {
        
    }
}