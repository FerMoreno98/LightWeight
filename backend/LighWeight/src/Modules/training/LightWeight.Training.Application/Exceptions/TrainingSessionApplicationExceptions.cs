namespace LightWeight.Training.Application.Exceptions;

public sealed class TrainingSessionNotFoundApplicationException : TrainingApplicationException
{
    public TrainingSessionNotFoundApplicationException() : base("Training session not found")
    {

    }
}
public sealed class NoActiveMicrocycleApplicationException : TrainingApplicationException
{
    public NoActiveMicrocycleApplicationException() : base("There is no week in progress, plan one before training")
    {

    }
}
public sealed class TemplateSessionNotInCurrentWeekApplicationException : TrainingApplicationException
{
    public TemplateSessionNotInCurrentWeekApplicationException() : base("The session does not belong to the template of the week in progress")
    {

    }
}
public sealed class TrainingSessionAlreadyExistsApplicationException : TrainingApplicationException
{
    public TrainingSessionAlreadyExistsApplicationException() : base("This session has already been started this week")
    {

    }
}
