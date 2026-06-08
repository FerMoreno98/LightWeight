using LightWeight.Auth.Application.Exceptions;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.Auth.Application.Commands.LoginWithRefreshToken;

public sealed class LoginWithRefreshTokenCommandHandler : ICommandHandler<LoginWithRefreshTokenCommand, LoginRefreshTokenResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginWithRefreshTokenCommandHandler(IUserRepository userRepository, IClock clock, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _clock = clock;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginRefreshTokenResult> HandleAsync(LoginWithRefreshTokenCommand command, CancellationToken ct = default)
    {
        User? user = await _userRepository.GetByRefreshTokenAsync(command.RefreshToken) ?? throw new UserNotFoundException();
        // si el usuario no tiene dispositivos lanzo una excepcion de dominio
        DeviceToken device = user.GetDeviceByRefreshToken(command.RefreshToken);
        device.ValidateRefreshToken(command.RefreshToken,_clock.UtcNow);
        
        RefreshToken refreshToken= device.IssueRefreshToken
        (
            command.Ip,
            _clock.UtcNow
        );
        string jwt = _jwtTokenGenerator.GenerateToken(user);
        return new LoginRefreshTokenResult(jwt,refreshToken.Token);


    }
}