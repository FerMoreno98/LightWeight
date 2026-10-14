using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;

/// <summary>Starts a session of the template of the week in progress. Returns its id</summary>
public sealed record CreateTrainingSessionCommand
(
    Guid UserId,
    Guid TemplateSessionId,
    int MotivationLevel,
    int SleepLevel,
    int DOMSLevel,
    string? Comments
) : ICommand<Guid>;
