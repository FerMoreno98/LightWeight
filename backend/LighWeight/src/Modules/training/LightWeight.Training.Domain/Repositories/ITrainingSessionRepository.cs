using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

/// <summary>Every read of a session loads its sets</summary>
public interface ITrainingSessionRepository
{
    Task AddAsync(TrainingSession trainingSession, CancellationToken cancellationToken);
    /// <summary>Deletes the session; its sets are deleted with it</summary>
    void Remove(TrainingSession trainingSession);
    Task<TrainingSession?> GetByIdAsync(Guid TrainingSessionId);
    Task<List<TrainingSession>> GetByMicrocycleIdAsync(Guid MicrocycleId);
    /// <summary>The session of a week that follows the given session of the template (there is at most one)</summary>
    Task<TrainingSession?> GetByMicrocycleAndTemplateSessionAsync(Guid MicrocycleId, Guid TemplateSessionId);
    /// <summary>
    /// For each exercise, the sets of it performed in the most recent session of the given weeks that contains it
    /// </summary>
    /// <param name="MicrocycleIds">Weeks to search in (e.g. the weeks of the active macrocycle)</param>
    /// <param name="ExerciseIds">Exercises to look for</param>
    /// <param name="ExcludeSessionId">Session to ignore (usually the one in progress)</param>
    Task<List<ExercisePerformance>> GetLastSetsOfExercisesAsync
    (
        IReadOnlyCollection<Guid> MicrocycleIds,
        IReadOnlyCollection<Guid> ExerciseIds,
        Guid? ExcludeSessionId
    );
}

/// <summary>Sets of an exercise performed in one session</summary>
public sealed record ExercisePerformance(Guid ExerciseId, Guid TrainingSessionId, DateTime PerformedAt, IReadOnlyList<PerformedSet> Sets);

public sealed record PerformedSet(int Repetitions, decimal Weight, decimal RPE, bool IsBodyWeight);
