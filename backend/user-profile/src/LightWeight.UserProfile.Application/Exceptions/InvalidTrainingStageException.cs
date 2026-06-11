namespace LightWeight.UserProfile.Application.Exceptions;

public sealed class InvalidTrainingStageException : UserProfileApplicationException
{
    public InvalidTrainingStageException() : base("Invalid stage")
    {
        
    }
}