namespace LightWeight.Training.Application.Exceptions;

public sealed class ProgramNotFoundApplicationException : TrainingApplicationException
{
    public ProgramNotFoundApplicationException() : base("The program does not correspond to any program")
    {

    }
}
