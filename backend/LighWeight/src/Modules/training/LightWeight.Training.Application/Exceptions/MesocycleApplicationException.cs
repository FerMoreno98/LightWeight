namespace LightWeight.Training.Application.Exceptions;

public sealed class MesocycleNotFoundException : TrainingApplicationException
{
    public MesocycleNotFoundException(): base("this mesocycle does not belong to this user or does not exists")
    {
        
    }
}