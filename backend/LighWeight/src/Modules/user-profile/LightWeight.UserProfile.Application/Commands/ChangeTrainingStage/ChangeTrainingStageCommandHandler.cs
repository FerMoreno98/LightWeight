
using LightWeight.shared.Mediator;
using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;
using LightWeight.UserProfile.Domain.Uow;

namespace LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;

public sealed class ChangeTrainingStageCommandHandler : ICommandHandler<ChangeTrainingStageCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileUnitOfWork _uow;

    public ChangeTrainingStageCommandHandler
    (
        IUserRepository userRepository, 
        IUserProfileUnitOfWork uow
    )
    {
        _userRepository = userRepository;
        _uow = uow;
    }

    public async Task HandleAsync(ChangeTrainingStageCommand command, CancellationToken ct = default)
    {
        User? user= await _userRepository.FindByIdAsync(command.UserId) ?? throw new UserNotFoundException();
        var stage = Enum.Parse<TrainingStage>(command.Stage, ignoreCase: true);
        user?.ChangeStage(stage);
        await _uow.SaveChangesAsync(ct);
        
    }
}