using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public sealed class TemplateSetNotFoundApplicationException : TrainingApplicationException, INotFoundException
{
    public TemplateSetNotFoundApplicationException() : base("Set not found")
    {
        
    }
}