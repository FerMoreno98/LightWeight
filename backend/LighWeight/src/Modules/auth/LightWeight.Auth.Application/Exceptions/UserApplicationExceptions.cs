using LightWeight.shared.Exceptions;

namespace LightWeight.Auth.Application.Exceptions;

public sealed class UserNotFoundException : AuthApplicationException, INotFoundException
{
    public UserNotFoundException() : base("User not found")
    {
        
    }
}