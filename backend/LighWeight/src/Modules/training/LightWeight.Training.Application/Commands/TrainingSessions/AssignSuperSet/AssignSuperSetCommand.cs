using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.AssignSuperSet;

/// <summary>Joins registered sets of a session in progress in a superset, or separates them when SuperSetGroupId is null</summary>
public sealed record AssignSuperSetCommand
(
    Guid TrainingSessionId,
    Guid UserId,
    List<Guid> SetIds,
    Guid? SuperSetGroupId
) : ICommand;
