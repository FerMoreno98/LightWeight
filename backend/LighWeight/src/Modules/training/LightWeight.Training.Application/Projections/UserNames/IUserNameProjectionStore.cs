namespace LightWeight.Training.Application.Projections.UserNames;

/// <summary>
/// Persistence of the user name projection: one row per user, upserted on every event. It is not emptied by the
/// rebuild (a profile cannot be deleted, so there is nothing stale to remove): UserProfile answers the rebuild
/// and the upsert overwrites it
/// </summary>
public interface IUserNameProjectionStore
{
    Task UpsertAsync(UserNameProjection projection, CancellationToken ct = default);

    /// <summary>Null while the user has not completed the profile (or its event has not arrived)</summary>
    Task<string?> GetNameAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Names of the given users in one query. Users without a projected name are left out</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
