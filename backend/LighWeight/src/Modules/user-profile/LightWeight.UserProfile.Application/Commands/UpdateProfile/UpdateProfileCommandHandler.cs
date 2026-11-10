
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;
using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;
using LightWeight.UserProfile.Domain.Uow;

namespace LightWeight.UserProfile.Application.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileUnitOfWork _uow;
    private readonly IClock _clock;

    public UpdateProfileCommandHandler
    (
        IUserRepository userRepository,
        IUserProfileUnitOfWork uow,
        IClock clock
    )
    {
        _userRepository = userRepository;
        _uow = uow;
        _clock = clock;
    }

    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken ct = default)
    {
        User? user = await _userRepository.FindByIdAsync(command.UserId) ?? throw new UserNotFoundException();
        var sex = Enum.Parse<Sex>(command.Sex, ignoreCase: true);
        user.Modify(command.Name, sex, command.DateOfBirth, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);
    }
}
