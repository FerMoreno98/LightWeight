using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Dashboard.Infrastructure.Persistence;

public sealed class BodyMetricsProjectionStore : IBodyMetricsProjectionStore
{
    private readonly DashboardDbContext _context;

    public BodyMetricsProjectionStore(DashboardDbContext context)
        => _context = context;

    public Task UpsertWeightAsync(WeightProjection projection, CancellationToken ct = default)
        => UpsertAsync(_context.BodyWeights, projection, projection.MeasurementId, ct);

    public Task DeleteWeightAsync(Guid measurementId, Guid userId, CancellationToken ct = default)
        => _context.BodyWeights.Where(w => w.MeasurementId == measurementId && w.UserId == userId).ExecuteDeleteAsync(ct);

    public Task UpsertPerimetersAsync(PerimetersProjection projection, CancellationToken ct = default)
        => UpsertAsync(_context.BodyPerimeters, projection, projection.MeasurementId, ct);

    public Task DeletePerimetersAsync(Guid measurementId, Guid userId, CancellationToken ct = default)
        => _context.BodyPerimeters.Where(p => p.MeasurementId == measurementId && p.UserId == userId).ExecuteDeleteAsync(ct);

    public Task UpsertSkinFoldsAsync(SkinFoldsProjection projection, CancellationToken ct = default)
        => UpsertAsync(_context.BodySkinFolds, projection, projection.MeasurementId, ct);

    public Task DeleteSkinFoldsAsync(Guid measurementId, Guid userId, CancellationToken ct = default)
        => _context.BodySkinFolds.Where(s => s.MeasurementId == measurementId && s.UserId == userId).ExecuteDeleteAsync(ct);

    public async Task ClearUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _context.BodyWeights.Where(w => w.UserId == userId).ExecuteDeleteAsync(ct);
        await _context.BodyPerimeters.Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
        await _context.BodySkinFolds.Where(s => s.UserId == userId).ExecuteDeleteAsync(ct);
    }

    public Task<List<WeightProjection>> GetWeightsAsync(Guid userId, CancellationToken ct = default)
        => _context.BodyWeights.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.MeasuredAt)
            .ToListAsync(ct);

    public Task<List<PerimetersProjection>> GetPerimetersAsync(Guid userId, CancellationToken ct = default)
        => _context.BodyPerimeters.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.MeasuredAt)
            .ToListAsync(ct);

    public Task<List<SkinFoldsProjection>> GetSkinFoldsAsync(Guid userId, CancellationToken ct = default)
        => _context.BodySkinFolds.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.MeasuredAt)
            .ToListAsync(ct);

    // Find by id: overwrite every value if it exists, add it otherwise
    private async Task UpsertAsync<T>(DbSet<T> set, T projection, Guid measurementId, CancellationToken ct) where T : class
    {
        T? existing = await set.FindAsync([measurementId], ct);
        if (existing is null)
            set.Add(projection);
        else
            _context.Entry(existing).CurrentValues.SetValues(projection);

        await _context.SaveChangesAsync(ct);
    }
}
