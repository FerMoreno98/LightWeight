using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Mediator;
using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;

namespace LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;

public sealed class ChangeTrainingStageCommandHandler : ICommandHandler<ChangeTrainingStagesCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _uow;

    public ChangeTrainingStageCommandHandler(IUserRepository userRepository, IUnitOfWork uow)
    {
        _userRepository = userRepository;
        _uow = uow;
    }

    public async Task HandleAsync(ChangeTrainingStagesCommand command, CancellationToken ct = default)
    {
        User? user= await _userRepository.FindByIdAsync(command.UserId) ?? throw new UserNotFoundException();
        var stage = Enum.Parse<TrainingStage>(command.Stage, ignoreCase: true);
        user?.ChangeStage(stage);
        await _uow.SaveChangesAsync(ct);
        
    }
}