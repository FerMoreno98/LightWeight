using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.RemoveSet;

/// <summary>Removes a registered set of a session in progress</summary>
public sealed record RemoveSetCommand(Guid TrainingSessionId, Guid SetId, Guid UserId) : ICommand;
