using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.TrainingSessions.GetPreviousPerformance;

/// <summary>
/// For each exercise, the sets of it performed in the most recent session of the active macrocycle that contains it,
/// whatever template that session followed
/// </summary>
/// <param name="ExcludeSessionId">Session to ignore (usually the one in progress)</param>
/// <param name="Before">Only sessions started before this moment (UTC): the start of the session being looked at, so an old
/// session is compared with the one before it and not with a later one. Null = the most recent</param>
public sealed record GetPreviousPerformanceQuery(IReadOnlyCollection<Guid> ExerciseIds, Guid? ExcludeSessionId, Guid UserId, DateTime? Before = null)
    : IQuery<List<GetPreviousPerformanceResponse>>;

/// <param name="PerformedAt">Start of the session the sets belong to</param>
public sealed record GetPreviousPerformanceResponse(Guid ExerciseId, DateTime PerformedAt, List<PreviousSetResponse> Sets);

public sealed record PreviousSetResponse(int Repetitions, decimal Weight, decimal RPE, bool IsBodyWeight);
