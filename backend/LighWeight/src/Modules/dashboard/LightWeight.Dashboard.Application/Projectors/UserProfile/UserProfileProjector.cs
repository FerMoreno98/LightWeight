using LightWeight.Dashboard.Application.Projections.UserProfile;
using LightWeight.shared.Contracts.UserProfile;
using LightWeight.shared.Messaging;

namespace LightWeight.Dashboard.Application.Projectors.UserProfile;

/// <summary>Keeps the sex of the user in sync with UserProfile. "Recorded" always upserts</summary>
public sealed class UserProfileProjector : IIntegrationEventHandler<ProfileRecordedIntegrationEvent>
{
    private readonly IUserProfileProjectionStore _store;

    public UserProfileProjector(IUserProfileProjectionStore store)
        => _store = store;

    public Task HandleAsync(ProfileRecordedIntegrationEvent @event, CancellationToken ct = default)
        => _store.UpsertAsync(new UserProfileProjection { UserId = @event.UserId, Sex = @event.Sex }, ct);
}
