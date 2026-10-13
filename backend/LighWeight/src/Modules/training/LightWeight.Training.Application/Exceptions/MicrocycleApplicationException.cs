namespace LightWeight.Training.Application.Exceptions;

public sealed class MicrocycleNotFoundException : TrainingApplicationException
{
    public MicrocycleNotFoundException(): base("this week does not belong to this user or does not exists")
    {

    }
}
