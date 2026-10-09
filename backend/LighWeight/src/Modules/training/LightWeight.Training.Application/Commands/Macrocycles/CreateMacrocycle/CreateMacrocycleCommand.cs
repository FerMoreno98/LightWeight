using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

/// <summary>Starts a new macrocycle for the user. Returns its id</summary>
public sealed record CreateMacrocycleCommand
(
    Guid UserId,
    string TrainingStage,
    string? Comments
) : ICommand<Guid>;
