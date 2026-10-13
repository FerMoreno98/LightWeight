using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Microcycles.FinishMicrocycle;

/// <summary>Finishes a week, so the next one can be planned in its mesocycle</summary>
public sealed record FinishMicrocycleCommand(Guid MicrocycleId, Guid UserId) : ICommand;
