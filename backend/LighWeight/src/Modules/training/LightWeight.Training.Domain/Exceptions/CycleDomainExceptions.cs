namespace LightWeight.Training.Domain.Exceptions;

public sealed class ActiveMacrocycleAlreadyExistsDomainException : TrainingDomainException
{
    public ActiveMacrocycleAlreadyExistsDomainException() : base("The user already has an active macrocycle, finish it before starting a new one")
    {

    }
}
public sealed class MacrocycleFinishedDomainException : TrainingDomainException
{
    public MacrocycleFinishedDomainException() : base("The macrocycle is already finished")
    {

    }
}
public sealed class ActiveMesocycleAlreadyExistsDomainException : TrainingDomainException
{
    public ActiveMesocycleAlreadyExistsDomainException() : base("The macrocycle already has an active mesocycle, finish it before starting a new one")
    {

    }
}
public sealed class MesocycleFinishedDomainException : TrainingDomainException
{
    public MesocycleFinishedDomainException() : base("The mesocycle is already finished")
    {

    }
}
