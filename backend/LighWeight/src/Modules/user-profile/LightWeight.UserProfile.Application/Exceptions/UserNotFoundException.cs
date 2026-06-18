namespace LightWeight.UserProfile.Application.Exceptions;

public sealed class UserNotFoundException : UserProfileApplicationException
{
    public UserNotFoundException() : base("User not found")
    {
        
    }
}