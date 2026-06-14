using LightWeight.shared.Mediator;

namespace LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;

public sealed record VerifyOtpCodeCommand(
    string Code,
    string Ip,
    string DeviceIdentifier,
    string DeviceName,
    string Platform,
    string Email
) : ICommand<OtpLoginResult>;

public record OtpLoginResult(string AccessToken, string RefreshToken);