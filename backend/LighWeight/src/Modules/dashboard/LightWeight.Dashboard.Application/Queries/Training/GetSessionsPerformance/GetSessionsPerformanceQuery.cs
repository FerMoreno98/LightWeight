using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;

/// <summary>Every session of the user, from the oldest, with its performance and the wellbeing it started with</summary>
public sealed record GetSessionsPerformanceQuery(Guid UserId) : IQuery<List<SessionPerformanceResponse>>;

/// <param name="Performance">Average change (%) against the previous time of its exercises; null if none could be compared</param>
/// <param name="ComparedExercises">Exercises that took part in the average</param>
/// <param name="Excluded">Exercises left out of the average because their change is not plausible (over ±50 %): to be checked</param>
public sealed record SessionPerformanceResponse
(
    Guid SessionId,
    string Name,
    DateTime StartedAt,
    decimal? Performance,
    int ComparedExercises,
    int Motivation,
    int Sleep,
    int Doms,
    List<ExcludedComparison> Excluded
);
