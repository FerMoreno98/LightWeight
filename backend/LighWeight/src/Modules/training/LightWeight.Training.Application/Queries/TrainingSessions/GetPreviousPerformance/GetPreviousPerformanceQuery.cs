using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.TrainingSessions.GetPreviousPerformance;

/// <summary>
/// For each exercise, the sets of it performed in the most recent session of the active macrocycle that contains it,
/// whatever template that session followed
/// </summary>
/// <param name="ExcludeSessionId">Session to ignore (usually the one in progress)</param>
public sealed record GetPreviousPerformanceQuery(IReadOnlyCollection<Guid> ExerciseIds, Guid? ExcludeSessionId, Guid UserId)
    : IQuery<List<GetPreviousPerformanceResponse>>;

/// <param name="PerformedAt">Start of the session the sets belong to</param>
public sealed record GetPreviousPerformanceResponse(Guid ExerciseId, DateTime PerformedAt, List<PreviousSetResponse> Sets);

public sealed record PreviousSetResponse(int Repetitions, decimal Weight, decimal RPE, bool IsBodyWeight);
