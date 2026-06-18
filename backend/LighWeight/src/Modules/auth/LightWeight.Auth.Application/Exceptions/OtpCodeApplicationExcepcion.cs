namespace LightWeight.Auth.Application.Exceptions;

public sealed class InvalidOtpCodeException : AuthApplicationException
{
    public InvalidOtpCodeException() : base("Invalid code")
    {
        
    }
}