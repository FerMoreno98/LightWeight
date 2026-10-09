using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

/// <summary>Starts a new mesocycle in a macrocycle, following a program. Returns its id</summary>
public sealed record CreateMesocycleCommand
(
    Guid MacrocycleId,
    Guid UserId,
    Guid ProgramId,
    int MotivationLevel,
    string? Injuries,
    string? Comments
) : ICommand<Guid>;
