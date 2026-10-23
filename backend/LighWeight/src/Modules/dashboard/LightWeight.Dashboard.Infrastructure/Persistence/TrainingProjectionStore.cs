using LightWeight.Dashboard.Application.Projections.Training;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Dashboard.Infrastructure.Persistence;

public sealed class TrainingProjectionStore : ITrainingProjectionStore
{
    private readonly DashboardDbContext _context;

    public TrainingProjectionStore(DashboardDbContext context)
        => _context = context;

    public async Task UpsertSessionAsync(TrainingSessionProjection session, IReadOnlyList<TrainingSetProjection> sets, CancellationToken ct = default)
    {
        TrainingSessionProjection? existing = await _context.TrainingSessions.FindAsync([session.SessionId], ct);
        if (existing is null)
            _context.TrainingSessions.Add(session);
        else
            _context.Entry(existing).CurrentValues.SetValues(session);

        // The sets are matched by id (overwrite, add, remove the ones that are not there any more) instead of
        // deleting and inserting: EF Core cannot track a deleted and a new entity with the same key at once
        var stored = await _context.TrainingSets.Where(s => s.SessionId == session.SessionId).ToListAsync(ct);
        var storedById = stored.ToDictionary(s => s.SetId);
        foreach (TrainingSetProjection set in sets)
        {
            if (storedById.Remove(set.SetId, out TrainingSetProjection? current))
                _context.Entry(current).CurrentValues.SetValues(set);
            else
                _context.TrainingSets.Add(set);
        }
        _context.TrainingSets.RemoveRange(storedById.Values);

        // One save: the session and its sets are written together
        await _context.SaveChangesAsync(ct);
    }

    public async Task ClearUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _context.TrainingSets.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
        await _context.TrainingSessions.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
    }

    public Task<List<TrainingSessionProjection>> GetSessionsAsync(Guid userId, CancellationToken ct = default)
        => _context.TrainingSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.StartedAt)
            .ToListAsync(ct);

    public Task<List<TrainingSetProjection>> GetSetsAsync(Guid userId, Guid? exerciseId = null, CancellationToken ct = default)
        => _context.TrainingSets.AsNoTracking()
            .Where(s => s.UserId == userId && (exerciseId == null || s.ExerciseId == exerciseId))
            .OrderBy(s => s.SessionId).ThenBy(s => s.Order)
            .ToListAsync(ct);
}
