using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;

public sealed record CreateTrainingSessionCommand
(
    Guid MicrocycleId,
    string Name,
    string? Comments,
    int MotivationLevel,
    int SleepLevel,
    int DOMSLevel
) : ICommand;
