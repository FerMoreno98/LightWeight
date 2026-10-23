using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetExerciseProgress;

/// <summary>The whole history of one exercise of the user. Null when the user has never performed it</summary>
public sealed record GetExerciseProgressQuery(Guid UserId, Guid ExerciseId) : IQuery<ExerciseProgressResponse?>;

public sealed record ExerciseProgressResponse
(
    Guid ExerciseId,
    string Name,
    List<ProgressionPoint> Progression,
    List<FatigueResponse> Fatigue,
    List<RecentSession> RecentSessions
);

/// <param name="EstimatedOneRepMax">Best estimated 1RM of the session (null if no set could be estimated)</param>
/// <param name="BestWeight">Highest load moved (null for body weight only)</param>
/// <param name="LowPrecision">Whether the 1RM comes from a long set (reps + RIR over 15)</param>
public sealed record ProgressionPoint
(
    DateTime PerformedAt,
    string SessionName,
    string Zone,
    decimal? EstimatedOneRepMax,
    decimal? BestWeight,
    bool LowPrecision,
    int Motivation,
    int Sleep,
    int Doms
);

/// <param name="Zone">"Strength", "Hypertrophy" or "Endurance"</param>
/// <param name="FirstToSecond">Performance lost from the first to the second set, in %</param>
/// <param name="FirstToLast">Performance lost from the first to the last set, in %</param>
public sealed record FatigueResponse(DateTime PerformedAt, string Zone, decimal FirstToSecond, decimal FirstToLast);

public sealed record RecentSession(DateTime PerformedAt, string SessionName, string Zone, List<RecentSet> Sets);

public sealed record RecentSet(int Order, decimal Weight, int Repetitions, decimal Rpe, bool IsBodyWeight, string? Technique);
