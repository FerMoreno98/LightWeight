using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

public sealed record CreateMacrocycleCommand
(
    Guid UserId,
    List<string> AimMuscleGroups,
    DateTime StartAt,
    DateTime? EndAt,
    string TrainingStage,
    string Periodization,
    string? Comments
) : ICommand;