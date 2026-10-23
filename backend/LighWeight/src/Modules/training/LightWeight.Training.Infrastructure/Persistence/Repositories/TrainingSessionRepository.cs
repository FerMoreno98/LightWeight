using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Repositories;

/// <summary>Always loads the sessions with their sets, in the order they were performed</summary>
public class TrainingSessionRepository : ITrainingSessionRepository
{
    private readonly TrainingDbContext _dbContext;

    public TrainingSessionRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<TrainingSession> SessionsWithSets =>
        _dbContext.TrainingSessions
            .Include(t => t.Sets.OrderBy(s => s.Id));

    public async Task AddAsync(TrainingSession trainingSession, CancellationToken cancellationToken)
    {
        await _dbContext.AddAsync(trainingSession, cancellationToken);
    }

    public void Remove(TrainingSession trainingSession)
    {
        // The sets are loaded with the session and the relationship cascades, so EF Core deletes them too
        _dbContext.TrainingSessions.Remove(trainingSession);
    }

    public async Task<TrainingSession?> GetByIdAsync(Guid TrainingSessionId)
    {
        return await SessionsWithSets.SingleOrDefaultAsync(t => t.Id == TrainingSessionId);
    }

    public async Task<List<TrainingSession>> GetByMicrocycleIdAsync(Guid MicrocycleId)
    {
        return await SessionsWithSets
            .Where(t => t.MicrocycleId == MicrocycleId)
            .ToListAsync();
    }

    public async Task<TrainingSession?> GetByMicrocycleAndTemplateSessionAsync(Guid MicrocycleId, Guid TemplateSessionId)
    {
        return await SessionsWithSets
            .SingleOrDefaultAsync(t => t.MicrocycleId == MicrocycleId && t.TemplateSessionId == TemplateSessionId);
    }

    public async Task<List<TrainingSession>> GetFinishedOfAUserAsync(Guid UserId)
    {
        return await SessionsWithSets
            .Where(t => t.UserId == UserId && t.FinishedAt != null)
            .OrderBy(t => t.StartAt)
            .ToListAsync();
    }

    public async Task<List<ExercisePerformance>> GetLastSetsOfExercisesAsync
    (
        IReadOnlyCollection<Guid> MicrocycleIds,
        IReadOnlyCollection<Guid> ExerciseIds,
        Guid? ExcludeSessionId
    )
    {
        if(MicrocycleIds.Count == 0 || ExerciseIds.Count == 0) return new();
        List<Guid> microcycleIds = MicrocycleIds.ToList();
        List<Guid> exerciseIds = ExerciseIds.Distinct().ToList();

        // Only the sets of the requested exercises are read; picking the latest session per exercise is done in memory
        var rows = await _dbContext.TrainingSessions
            .Where(t => microcycleIds.Contains(t.MicrocycleId) && t.Id != ExcludeSessionId)
            .SelectMany(t => t.Sets
                .Where(s => exerciseIds.Contains(s.ExerciseId))
                .Select(s => new
                {
                    s.ExerciseId,
                    SessionId = t.Id,
                    t.StartAt,
                    SetId = s.Id,
                    s.Repetitions,
                    s.Weight,
                    s.RPE,
                    s.IsBodyWeight
                }))
            .ToListAsync();

        return rows
            .GroupBy(r => r.ExerciseId)
            .Select(exercise =>
            {
                var lastSession = exercise
                    .GroupBy(r => new { r.SessionId, r.StartAt })
                    .MaxBy(s => s.Key.StartAt)!;
                return new ExercisePerformance
                (
                    exercise.Key,
                    lastSession.Key.SessionId,
                    lastSession.Key.StartAt,
                    lastSession
                        .OrderBy(r => r.SetId)
                        .Select(r => new PerformedSet(r.Repetitions, r.Weight, r.RPE, r.IsBodyWeight))
                        .ToList()
                );
            })
            .ToList();
    }
}
