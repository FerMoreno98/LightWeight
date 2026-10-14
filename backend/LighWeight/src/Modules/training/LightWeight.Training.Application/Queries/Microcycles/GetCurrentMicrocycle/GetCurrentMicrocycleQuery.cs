using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Microcycles.GetCurrentMicrocycle;

/// <summary>
/// The week the user is training right now: the active week of the active mesocycle of the active macrocycle.
/// When there is none, Status says which step is missing so the home can ask for it
/// </summary>
public sealed record GetCurrentMicrocycleQuery(Guid UserId) : IQuery<GetCurrentMicrocycleResponse>;

/// <summary>Which level of the hierarchy is missing, or InProgress when there is an active week</summary>
public static class CurrentMicrocycleStatus
{
    public const string NoActiveMacrocycle = "NoActiveMacrocycle";
    public const string NoActiveMesocycle = "NoActiveMesocycle";
    /// <summary>There is an active mesocycle but its next week has not been planned yet</summary>
    public const string PendingPlanning = "PendingPlanning";
    public const string InProgress = "InProgress";
}

/// <param name="Status">One of <see cref="CurrentMicrocycleStatus"/></param>
/// <param name="NextWeekNumber">Number the next planned week will get (only with PendingPlanning)</param>
/// <param name="Microcycle">The active week (only with InProgress)</param>
/// <param name="TrainingSessions">Sessions started in the active week (empty unless InProgress)</param>
public sealed record GetCurrentMicrocycleResponse
(
    string Status,
    Guid? MacrocycleId,
    Guid? MesocycleId,
    Guid? ProgramId,
    string? ProgramName,
    int? NextWeekNumber,
    CurrentMicrocycleResponse? Microcycle,
    List<CurrentWeekSessionResponse> TrainingSessions
);

/// <param name="FinishedAt">Null while the session is in progress</param>
public sealed record CurrentWeekSessionResponse(Guid Id, Guid? TemplateSessionId, DateTime? FinishedAt);

/// <param name="TemplateName">Null when the template has been deleted</param>
public sealed record CurrentMicrocycleResponse
(
    Guid Id,
    int WeekNumber,
    DateTime StartedAt,
    Guid TrainingTemplateId,
    string? TemplateName,
    int? DurationInDays
);
