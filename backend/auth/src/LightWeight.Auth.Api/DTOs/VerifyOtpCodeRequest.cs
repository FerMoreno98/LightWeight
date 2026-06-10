namespace LightWeight.Auth.Api.DTOs;

public record VerifyOtpCodeRequest
{
    public string Code{get;set;}
    public string DeviceIdentifier{get;set;}
    public string DeviceName{get;set;}
    public string Platform {get;set;}
    public string Email {get;set;}
}