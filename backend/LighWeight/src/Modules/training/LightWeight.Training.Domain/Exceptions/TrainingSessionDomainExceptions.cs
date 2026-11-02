using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Domain.Exceptions;

public sealed class TrainingSessionFinishedDomainException : TrainingDomainException, IConflictException
{
    public TrainingSessionFinishedDomainException() : base("The training session is already finished")
    {

    }
}
public sealed class TemplateSetAlreadyRegisteredDomainException : TrainingDomainException, IConflictException
{
    public TemplateSetAlreadyRegisteredDomainException() : base("This planned set is already registered in the session, update it instead")
    {

    }
}
public sealed class PerformedSetNotFoundDomainException : TrainingDomainException, INotFoundException
{
    public PerformedSetNotFoundDomainException() : base("The set does not belong to this training session")
    {

    }
}
public sealed class InvalidSetValuesDomainException : TrainingDomainException
{
    public InvalidSetValuesDomainException() : base("Repetitions and weight cannot be negative and the RPE must be between 1 and 10")
    {

    }
}
