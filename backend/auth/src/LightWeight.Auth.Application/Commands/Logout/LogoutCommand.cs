using LightWeight.shared.Mediator;

namespace LightWeight.Auth.Application.Commands.Logout;

public sealed record LogoutCommand(Guid UserId,string RefreshToken) : ICommand;