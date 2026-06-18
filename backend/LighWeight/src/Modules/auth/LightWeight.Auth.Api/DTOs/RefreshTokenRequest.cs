namespace LightWeight.Auth.Api.DTOs;

public record RefreshTokenRequest(string RefreshToken);

public record RefreshTokenResponse(string AccessToken,string RefreshToken);