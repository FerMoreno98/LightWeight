namespace LightWeight.Dashboard.Application.Projections.Training;

/// <summary>
/// Persistence of the training projections. Writing a session can be repeated safely: it overwrites the session
/// and leaves exactly the given sets (no duplicates)
/// </summary>
public interface ITrainingProjectionStore
{
    /// <summary>Upserts the session and replaces its sets, in one save</summary>
    Task UpsertSessionAsync(TrainingSessionProjection session, IReadOnlyList<TrainingSetProjection> sets, CancellationToken ct = default);

    /// <summary>Deletes every training projection of the user, before rebuilding them</summary>
    Task ClearUserAsync(Guid userId, CancellationToken ct = default);

    // Reads: the whole history of the user

    /// <summary>Sessions of the user, from the oldest</summary>
    Task<List<TrainingSessionProjection>> GetSessionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Sets of the user (all of them, or only the ones of an exercise), by session and order</summary>
    Task<List<TrainingSetProjection>> GetSetsAsync(Guid userId, Guid? exerciseId = null, CancellationToken ct = default);
}
