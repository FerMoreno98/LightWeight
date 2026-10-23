using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetExercisesProgress;

/// <summary>The exercises the user has performed, from the most recently trained, with a summary of their progress</summary>
public sealed record GetExercisesProgressQuery(Guid UserId) : IQuery<List<ExerciseProgressSummary>>;

/// <param name="Sessions">Sessions in which the exercise was performed</param>
/// <param name="LastEstimatedOneRepMax">Best estimated 1RM of the last session that has one (null for body weight exercises)</param>
/// <param name="Trend">Change (%) of that 1RM against the previous session in the same zone; null if there is none</param>
public sealed record ExerciseProgressSummary
(
    Guid ExerciseId,
    string Name,
    DateTime LastPerformedAt,
    int Sessions,
    decimal? LastEstimatedOneRepMax,
    decimal? Trend
);
