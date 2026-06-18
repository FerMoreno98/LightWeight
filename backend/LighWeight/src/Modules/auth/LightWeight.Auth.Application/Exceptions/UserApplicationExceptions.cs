namespace LightWeight.Auth.Application.Exceptions;

public sealed class UserNotFoundException : AuthApplicationException
{
    public UserNotFoundException() : base("User not found")
    {
        
    }
}