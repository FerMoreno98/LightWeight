using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.DiscardTrainingSession;

/// <summary>Deletes a session in progress with all its sets, so it can be started again</summary>
public sealed record DiscardTrainingSessionCommand(Guid TrainingSessionId, Guid UserId) : ICommand;
