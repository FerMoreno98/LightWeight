using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public sealed class ProgramNotFoundApplicationException : TrainingApplicationException, INotFoundException
{
    public ProgramNotFoundApplicationException() : base("The program does not correspond to any program")
    {

    }
}
