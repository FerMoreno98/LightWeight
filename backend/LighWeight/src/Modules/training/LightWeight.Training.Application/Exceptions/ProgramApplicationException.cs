using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public sealed class ProgramNotFoundApplicationException : TrainingApplicationException, INotFoundException
{
    public ProgramNotFoundApplicationException() : base("The program does not correspond to any program")
    {

    }
}

/// <summary>No program is shared with the token: it never existed or it was revoked (both give the same 404)</summary>
public sealed class SharedProgramNotFoundApplicationException : TrainingApplicationException, INotFoundException
{
    public SharedProgramNotFoundApplicationException() : base("The link does not correspond to any shared program")
    {

    }
}
