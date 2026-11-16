using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetExercisesProgress;

/// <summary>The exercises the user has performed, from the most recently trained, with a summary of their progress</summary>
/// <param name="ExercisesMacrocycleId">Only the exercises performed in this macrocycle (null: every exercise ever performed)</param>
/// <param name="MarksMacrocycleId">
/// Sessions, 1RM and trend computed only with the sessions of this macrocycle (null: the whole history). Independent
/// of the exercises filter, so the exercises of the current macrocycle can be shown with their historical marks
/// </param>
public sealed record GetExercisesProgressQuery(Guid UserId, Guid? ExercisesMacrocycleId = null, Guid? MarksMacrocycleId = null)
    : IQuery<List<ExerciseProgressSummary>>;

/// <param name="LastPerformedAt">Last time it was performed, in the whole history</param>
/// <param name="Sessions">Sessions in which the exercise was performed (0 when it has none in the macrocycle of the marks)</param>
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
