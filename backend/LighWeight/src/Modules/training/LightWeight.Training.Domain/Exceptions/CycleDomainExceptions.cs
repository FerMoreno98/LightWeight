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
public sealed class ActiveMicrocycleAlreadyExistsDomainException : TrainingDomainException
{
    public ActiveMicrocycleAlreadyExistsDomainException() : base("The mesocycle already has an active week, finish it before planning the next one")
    {

    }
}
public sealed class MicrocycleFinishedDomainException : TrainingDomainException
{
    public MicrocycleFinishedDomainException() : base("The week is already finished")
    {

    }
}
public sealed class MesocycleNotFoundDomainException : TrainingDomainException
{
    public MesocycleNotFoundDomainException() : base("The mesocycle does not belong to this macrocycle")
    {

    }
}
public sealed class MicrocycleNotFoundDomainException : TrainingDomainException
{
    public MicrocycleNotFoundDomainException() : base("The week does not belong to this macrocycle")
    {

    }
}
