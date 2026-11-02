using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public sealed class ExerciseNotFounApplicationException : TrainingApplicationException, INotFoundException
{
    public ExerciseNotFounApplicationException() : base("Exercise not found")
    {
        
    }
}