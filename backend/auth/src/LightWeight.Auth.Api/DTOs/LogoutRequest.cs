namespace LightWeight.Auth.Api.DTOs;

public record LogoutRequest
{
    public Guid UserId{get;set;}
    public string RefreshToken{get;set;}
}