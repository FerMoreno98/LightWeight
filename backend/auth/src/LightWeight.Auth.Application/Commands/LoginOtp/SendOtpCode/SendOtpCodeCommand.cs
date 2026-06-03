using LightWeight.shared.Mediator;

namespace auth.Application.Commands.LoginOtp.SendOtpCode;

public sealed record SendOtpCodeCommand(string Email) : ICommand;