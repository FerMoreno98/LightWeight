using LightWeight.Dashboard.Application.Projections.BodyMetrics;

namespace LightWeight.Dashboard.UnitTests.Fakes;

/// <summary>Store with the same semantics as the real one (upsert by MeasurementId, deletes filtered by user), kept in memory</summary>
internal sealed class InMemoryBodyMetricsProjectionStore : IBodyMetricsProjectionStore
{
    public Dictionary<Guid, WeightProjection> Weights { get; } = [];
    public Dictionary<Guid, PerimetersProjection> Perimeters { get; } = [];
    public Dictionary<Guid, SkinFoldsProjection> SkinFolds { get; } = [];

    public Task UpsertWeightAsync(WeightProjection projection, CancellationToken ct = default)
    {
        Weights[projection.MeasurementId] = projection;
        return Task.CompletedTask;
    }

    public Task DeleteWeightAsync(Guid measurementId, Guid userId, CancellationToken ct = default)
        => Delete(Weights, measurementId, w => w.UserId == userId);

    public Task UpsertPerimetersAsync(PerimetersProjection projection, CancellationToken ct = default)
    {
        Perimeters[projection.MeasurementId] = projection;
        return Task.CompletedTask;
    }

    public Task DeletePerimetersAsync(Guid measurementId, Guid userId, CancellationToken ct = default)
        => Delete(Perimeters, measurementId, p => p.UserId == userId);

    public Task UpsertSkinFoldsAsync(SkinFoldsProjection projection, CancellationToken ct = default)
    {
        SkinFolds[projection.MeasurementId] = projection;
        return Task.CompletedTask;
    }

    public Task DeleteSkinFoldsAsync(Guid measurementId, Guid userId, CancellationToken ct = default)
        => Delete(SkinFolds, measurementId, s => s.UserId == userId);

    public Task ClearUserAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (var id in Weights.Where(w => w.Value.UserId == userId).Select(w => w.Key).ToList()) Weights.Remove(id);
        foreach (var id in Perimeters.Where(p => p.Value.UserId == userId).Select(p => p.Key).ToList()) Perimeters.Remove(id);
        foreach (var id in SkinFolds.Where(s => s.Value.UserId == userId).Select(s => s.Key).ToList()) SkinFolds.Remove(id);
        return Task.CompletedTask;
    }

    private static Task Delete<T>(Dictionary<Guid, T> rows, Guid measurementId, Func<T, bool> ofUser)
    {
        if (rows.TryGetValue(measurementId, out T? row) && ofUser(row))
            rows.Remove(measurementId);
        return Task.CompletedTask;
    }
}
