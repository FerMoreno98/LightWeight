using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.RegisterSet;

/// <summary>
/// Registers a performed set in a session. With TemplateSetId it covers that planned set (possibly with another exercise);
/// without it, it is an extra set. SuperSetGroupId is the superset it was performed in: the planned one, one improvised
/// in the session or none. Returns its id
/// </summary>
public sealed record RegisterSetCommand
(
    Guid TrainingSessionId,
    Guid UserId,
    Guid ExerciseId,
    Guid? TemplateSetId,
    int Repetitions,
    decimal Weight,
    decimal RPE,
    bool IsBodyWeight,
    Guid? SuperSetGroupId = null
) : ICommand<Guid>;
