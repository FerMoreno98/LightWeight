using LightWeight.shared.Contracts.UserProfile;
using LightWeight.shared.Messaging;
using LightWeight.Training.Application.Projections.UserNames;

namespace LightWeight.Training.Application.Events;

/// <summary>Keeps the name of the user in sync with UserProfile. "Recorded" always upserts</summary>
public sealed class UserProfileProjector : IIntegrationEventHandler<ProfileRecordedIntegrationEvent>
{
    private readonly IUserNameProjectionStore _store;

    public UserProfileProjector(IUserNameProjectionStore store)
        => _store = store;

    public Task HandleAsync(ProfileRecordedIntegrationEvent @event, CancellationToken ct = default)
        => _store.UpsertAsync(new UserNameProjection { UserId = @event.UserId, Name = @event.Name }, ct);
}
