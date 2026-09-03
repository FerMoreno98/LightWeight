using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

public sealed record CreateMesocycleCommand
(
    Guid MacrocycleId,
    Guid UserId,
    List<string> AimMuscles,
    int MotivationLevel,
    string? Injuries,
    string? Comments,
    DateTime StartAt,
    DateTime EndAt

) : ICommand;