using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;

/// <summary>Finishes a mesocycle, so a new one can be started in its macrocycle</summary>
public sealed record FinishMesocycleCommand(Guid MesocycleId, Guid UserId) : ICommand;
