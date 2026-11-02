using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Domain.Exceptions;

public sealed class ActiveMacrocycleAlreadyExistsDomainException : TrainingDomainException, IConflictException
{
    public ActiveMacrocycleAlreadyExistsDomainException() : base("The user already has an active macrocycle, finish it before starting a new one")
    {

    }
}
public sealed class MacrocycleFinishedDomainException : TrainingDomainException, IConflictException
{
    public MacrocycleFinishedDomainException() : base("The macrocycle is already finished")
    {

    }
}
public sealed class ActiveMesocycleAlreadyExistsDomainException : TrainingDomainException, IConflictException
{
    public ActiveMesocycleAlreadyExistsDomainException() : base("The macrocycle already has an active mesocycle, finish it before starting a new one")
    {

    }
}
public sealed class MesocycleFinishedDomainException : TrainingDomainException, IConflictException
{
    public MesocycleFinishedDomainException() : base("The mesocycle is already finished")
    {

    }
}
public sealed class ActiveMicrocycleAlreadyExistsDomainException : TrainingDomainException, IConflictException
{
    public ActiveMicrocycleAlreadyExistsDomainException() : base("The mesocycle already has an active week, finish it before planning the next one")
    {

    }
}
public sealed class MicrocycleFinishedDomainException : TrainingDomainException, IConflictException
{
    public MicrocycleFinishedDomainException() : base("The week is already finished")
    {

    }
}
public sealed class MesocycleNotFoundDomainException : TrainingDomainException, INotFoundException
{
    public MesocycleNotFoundDomainException() : base("The mesocycle does not belong to this macrocycle")
    {

    }
}
public sealed class MicrocycleNotFoundDomainException : TrainingDomainException, INotFoundException
{
    public MicrocycleNotFoundDomainException() : base("The week does not belong to this macrocycle")
    {

    }
}
