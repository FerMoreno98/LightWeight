using LightWeight.Dashboard.Application.Projections.UserProfile;

namespace LightWeight.Dashboard.UnitTests.Fakes;

/// <summary>Store with the same semantics as the real one (upsert by UserId), kept in memory</summary>
internal sealed class InMemoryUserProfileProjectionStore : IUserProfileProjectionStore
{
    public Dictionary<Guid, UserProfileProjection> Profiles { get; } = [];

    public Task UpsertAsync(UserProfileProjection projection, CancellationToken ct = default)
    {
        Profiles[projection.UserId] = projection;
        return Task.CompletedTask;
    }

    public Task<string?> GetSexAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Profiles.TryGetValue(userId, out var profile) ? profile.Sex : null);
}
