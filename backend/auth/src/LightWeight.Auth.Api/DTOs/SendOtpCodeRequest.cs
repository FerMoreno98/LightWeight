namespace LightWeight.Auth.Api.DTOs;

public record SendOtpCodeRequest
{
    public string Email {get; set;}
}