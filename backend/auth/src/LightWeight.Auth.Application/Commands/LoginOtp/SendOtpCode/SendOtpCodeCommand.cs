using LightWeight.shared.Mediator;

namespace LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;

public sealed record SendOtpCodeCommand(string Email) : ICommand;