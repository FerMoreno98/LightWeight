

namespace LightWeight.Auth.Domain.Exceptions;

public sealed class StolenRefreshTokenException : AuthDomainException
{
    public Guid UserId {get;}
    public StolenRefreshTokenException(Guid userId):base("Possible refresh token stole")
    {
        UserId = userId;
    }
}

public sealed class RevokedRefreshTokenException : AuthDomainException
{
    public RevokedRefreshTokenException() : base("Token Revoked")
    {
        
    }
}

public sealed class InvalidRefreshTokenException : AuthDomainException
{
    public InvalidRefreshTokenException() : base("Invalid token")
    {
        
    }
}

public sealed class RefreshTokenNotFoundException : AuthDomainException
{
    public RefreshTokenNotFoundException() : base("Token not found")
    {
        
    }
}