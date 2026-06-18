namespace LightWeight.Auth.Api.DTOs;

public record VerifyOtpCodeRequest(string Code,string DeviceIdentifier,string DeviceName,string Platform,string Email);

public record OtpLoginResponse(string RefreshToken,string AccessToken);
