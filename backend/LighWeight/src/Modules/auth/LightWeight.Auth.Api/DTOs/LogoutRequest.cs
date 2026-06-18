namespace LightWeight.Auth.Api.DTOs;

public record LogoutRequest(Guid UserId,string RefreshToken);