using LightWeight.shared.Exceptions;

namespace LightWeight.UserProfile.Application.Exceptions;

public sealed class UserNotFoundException : UserProfileApplicationException, INotFoundException
{
    public UserNotFoundException() : base("User not found")
    {
        
    }
}