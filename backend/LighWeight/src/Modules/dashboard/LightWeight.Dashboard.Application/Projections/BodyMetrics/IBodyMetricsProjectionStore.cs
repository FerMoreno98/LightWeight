namespace LightWeight.Dashboard.Application.Projections.BodyMetrics;

/// <summary>
/// Persistence of the bodymetrics projections. Every operation saves at once (one event, one write), and all of
/// them can be repeated safely: upserting an existing measurement overwrites it and deleting a missing one does nothing
/// </summary>
public interface IBodyMetricsProjectionStore
{
    Task UpsertWeightAsync(WeightProjection projection, CancellationToken ct = default);
    Task DeleteWeightAsync(Guid measurementId, Guid userId, CancellationToken ct = default);

    Task UpsertPerimetersAsync(PerimetersProjection projection, CancellationToken ct = default);
    Task DeletePerimetersAsync(Guid measurementId, Guid userId, CancellationToken ct = default);

    Task UpsertSkinFoldsAsync(SkinFoldsProjection projection, CancellationToken ct = default);
    Task DeleteSkinFoldsAsync(Guid measurementId, Guid userId, CancellationToken ct = default);

    /// <summary>Deletes every bodymetrics projection of the user, before rebuilding them</summary>
    Task ClearUserAsync(Guid userId, CancellationToken ct = default);
}
