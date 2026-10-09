using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;

/// <summary>Adds the next week to a mesocycle using a template of its program. Returns its id</summary>
public sealed record CreateMicrocycleCommand
(
    Guid MesocycleId,
    Guid UserId,
    Guid TrainingTemplateId
) : ICommand<Guid>;
