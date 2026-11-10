namespace LightWeight.Dashboard.Application.Projections.UserProfile;

/// <summary>
/// Persistence of the profile projection: one row per user, upserted on every event. It is not emptied by the
/// rebuild (a profile cannot be deleted, so there is nothing stale to remove): UserProfile answers the rebuild
/// and the upsert overwrites it
/// </summary>
public interface IUserProfileProjectionStore
{
    Task UpsertAsync(UserProfileProjection projection, CancellationToken ct = default);

    /// <summary>Null while the user has not completed the profile (or its event has not arrived)</summary>
    Task<string?> GetSexAsync(Guid userId, CancellationToken ct = default);
}
