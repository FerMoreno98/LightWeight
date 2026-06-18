using LightWeight.shared.Mediator;

namespace LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;

public sealed record ChangeTrainingStageCommand
(
    Guid UserId,
    string Stage
) : ICommand;