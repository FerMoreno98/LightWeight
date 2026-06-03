using LightWeight.shared.Mediator;

namespace auth.Application.Commands.LoginOtp.SendOtpCode;


public sealed class SendOtpCodeCommandHandler : ICommandHandler<SendOtpCodeCommand>
{
    public Task HandleAsync(SendOtpCodeCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}