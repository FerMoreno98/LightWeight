namespace LightWeight.UserProfile.Application.Exceptions;

public sealed class InvalidSexException : UserProfileApplicationException
{
    public InvalidSexException() : base("Invalid sex")
    {
    }
}