
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;
using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;
using LightWeight.UserProfile.Domain.Uow;

namespace LightWeight.UserProfile.Application.Commands.CompleteProfile;

public sealed class CompleteProfileCommandHandler : ICommandHandler<CompleteProfileCommand>
{
    private readonly IUserProfileUnitOfWork _uow;
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;

    public CompleteProfileCommandHandler
    (
        IUserProfileUnitOfWork uow, 
        IUserRepository userRepository,
        IClock clock
    )
    {
        _uow = uow;
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task HandleAsync(CompleteProfileCommand command, CancellationToken ct = default)
    {
        var stage = Enum.Parse<TrainingStage>(command.CurrentStage, ignoreCase: true);
        var sex   = Enum.Parse<Sex>(command.Sex, ignoreCase: true);
        User user = User.Create
        (
            command.UserId,
            command.Name,
            sex,
            command.DateOfBirth,
            stage,
            _clock.UtcNow
        );
        await _userRepository.AddAsync(user,ct);
        await _uow.SaveChangesAsync(ct);
    }
}