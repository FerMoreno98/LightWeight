using LightWeight.Dashboard.Application.Calculations.Nutrition;
using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;

/// <summary>Every session of the user, from the oldest, with its performance and the wellbeing it started with</summary>
/// <param name="MacrocycleId">Only the sessions of this macrocycle, each one compared only with the sessions of it
/// (null: the whole history, compared with the previous time whatever the macrocycle)</param>
public sealed record GetSessionsPerformanceQuery(Guid UserId, Guid? MacrocycleId = null) : IQuery<List<SessionPerformanceResponse>>;

/// <param name="Performance">Average change (%) against the previous time of its exercises; null if none could be compared</param>
/// <param name="ComparedExercises">Exercises that took part in the average</param>
/// <param name="Excluded">Exercises left out of the average because their change is not plausible (over ±50 %): to be checked</param>
/// <param name="Nutrition">What the user ate around the session (FatSecret diary); null without diary around it</param>
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
    List<ExcludedComparison> Excluded,
    SessionNutrition? Nutrition
);
