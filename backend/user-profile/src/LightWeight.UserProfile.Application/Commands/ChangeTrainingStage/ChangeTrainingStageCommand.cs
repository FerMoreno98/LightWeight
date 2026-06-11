using LightWeight.shared.Mediator;

namespace LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;

public sealed record ChangeTrainingStagesCommand
(
    Guid UserId,
    string Stage
) : ICommand;