
using LightWeight.shared.Mediator;

namespace LightWeight.Auth.Application.Commands.LoginWithRefreshToken;

public sealed record LoginWithRefreshTokenCommand
(
    string RefreshToken,
    string Ip
) : ICommand<LoginRefreshTokenResult>;

public record LoginRefreshTokenResult(string AccessToken, string RefreshToken);