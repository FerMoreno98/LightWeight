using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

public sealed record CreateMacrocycleCommand
(
    Guid UserId,
    DateTime StartAt,
    DateTime? EndAt,
    string TrainingStage,
    string Periodization,
    string? Comments
) : ICommand;