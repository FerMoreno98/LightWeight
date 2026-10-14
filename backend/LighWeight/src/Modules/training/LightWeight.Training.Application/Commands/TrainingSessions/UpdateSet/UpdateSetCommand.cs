using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.UpdateSet;

/// <summary>Corrects a registered set of a session in progress, including the exercise performed</summary>
public sealed record UpdateSetCommand
(
    Guid TrainingSessionId,
    Guid SetId,
    Guid UserId,
    Guid ExerciseId,
    int Repetitions,
    decimal Weight,
    decimal RPE,
    bool IsBodyWeight
) : ICommand;
