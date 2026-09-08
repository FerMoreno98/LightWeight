namespace LightWeight.Training.Application.Exceptions;

public sealed class ExerciseNotFounApplicationException : TrainingApplicationException
{
    public ExerciseNotFounApplicationException() : base("Exercise not found")
    {
        
    }
}