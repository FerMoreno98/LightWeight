using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;

/// <summary>Finishes a macrocycle and its active mesocycle</summary>
public sealed record FinishMacrocycleCommand(Guid MacrocycleId, Guid UserId) : ICommand;
