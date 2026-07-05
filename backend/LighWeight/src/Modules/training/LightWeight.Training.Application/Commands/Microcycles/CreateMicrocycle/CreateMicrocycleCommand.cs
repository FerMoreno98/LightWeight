using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;

public sealed record CreateMicrocycleCommand
(
    Guid MesocycleId,
    int DurationInDays,
    string TrainingDistribution
) : ICommand;