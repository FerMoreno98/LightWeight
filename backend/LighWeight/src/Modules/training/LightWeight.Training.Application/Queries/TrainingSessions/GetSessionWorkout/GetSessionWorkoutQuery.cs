using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.TrainingSessions.GetSessionWorkout;

/// <summary>
/// What the user has done this week for a session of a template: the session in progress or finished (if any)
/// and whether it can be started now
/// </summary>
public sealed record GetSessionWorkoutQuery(Guid TemplateSessionId, Guid UserId) : IQuery<GetSessionWorkoutResponse>;

/// <param name="CanStart">True when the session belongs to the week in progress and has not been started yet</param>
/// <param name="Session">The session of the week in progress, null when it has not been started</param>
public sealed record GetSessionWorkoutResponse(bool CanStart, WorkoutSessionResponse? Session);

public sealed record WorkoutSessionResponse
(
    Guid Id,
    DateTime StartAt,
    DateTime? FinishedAt,
    TimeSpan Duration,
    int MotivationLevel,
    int SleepLevel,
    int DOMSLevel,
    string? Comments,
    List<WorkoutSetResponse> Sets
);

/// <param name="TemplateSetId">Planned set it covers, null for an extra set</param>
/// <param name="ExerciseName">Empty when the exercise no longer exists</param>
public sealed record WorkoutSetResponse
(
    Guid Id,
    Guid ExerciseId,
    string ExerciseName,
    Guid? TemplateSetId,
    int Repetitions,
    decimal Weight,
    decimal RPE,
    bool IsBodyWeight,
    Guid? SuperSetGroupId
);
