using LightWeight.shared.Mediator;

namespace auth.Application.Commands.LoginOtp.VerifyOtpCode;

public sealed record VerifyOtpCodeCommandHandler : ICommandHandler<VerifyOtpCodeCommand, LoginResult>
{
    public Task<LoginResult> HandleAsync(VerifyOtpCodeCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}