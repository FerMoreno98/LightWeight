using LightWeight.Auth.Application.Exceptions;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Repository;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.Auth.Application.Commands.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{ 
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;

    public LogoutCommandHandler(IUserRepository userRepository, IClock clock,IUnitOfWork uow)
    {
        _userRepository = userRepository;
        _clock = clock;
        _uow = uow;
    }

    public async Task HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        User? user = await _userRepository.FindbyIdAsync(command.UserId) ?? throw new UserNotFoundException();
        DeviceToken deviceToken = user.GetDeviceByRefreshToken(command.RefreshToken);
        deviceToken.RevokeRefreshToken(command.RefreshToken, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);
        // aqui revocamos el refresh pero no el access, el access generalmente se deja que caduque
    }
}