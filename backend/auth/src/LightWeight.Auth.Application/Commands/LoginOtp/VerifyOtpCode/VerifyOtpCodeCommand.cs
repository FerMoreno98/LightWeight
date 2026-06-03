using LightWeight.shared.Mediator;

namespace auth.Application.Commands.LoginOtp.VerifyOtpCode;

public sealed record VerifyOtpCodeCommand : ICommand<LoginResult>;

public record LoginResult(string AccessToken, string RefreshToken, bool IsExistingUser);