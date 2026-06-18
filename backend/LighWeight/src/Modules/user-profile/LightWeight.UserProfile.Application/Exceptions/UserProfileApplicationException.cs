namespace LightWeight.UserProfile.Application.Exceptions;

/// <summary>
/// Base class for all Application exceptions raised within the Auth bounded context.
/// Catching this type provides a single catch-all for any Auth domain violation.
/// </summary>
public abstract class UserProfileApplicationException : Exception
{
    protected UserProfileApplicationException(string message) : base(message)
    {
        
    }
}