using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.FinishTrainingSession;

/// <summary>
/// Finishes a session and stores its duration. A finished session is read only.
/// When it was the last pending session of the week's template, the week is finished too
/// </summary>
public sealed record FinishTrainingSessionCommand(Guid TrainingSessionId, Guid UserId) : ICommand<FinishTrainingSessionResponse>;

/// <param name="WeekFinished">True when finishing this session completed and finished the week</param>
/// <param name="WeekNumber">Number of the week the session belongs to</param>
public sealed record FinishTrainingSessionResponse(bool WeekFinished, int WeekNumber);
