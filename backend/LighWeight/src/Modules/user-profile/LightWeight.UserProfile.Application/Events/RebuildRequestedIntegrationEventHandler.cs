using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Contracts.UserProfile;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using LightWeight.UserProfile.Domain.Repository;

namespace LightWeight.UserProfile.Application.Events;

/// <summary>
/// The dashboard asks to publish again the current data of a user: publish the profile, if it was completed,
/// so a lost event is repaired with the rebuild like the rest of the sources
/// </summary>
public sealed class RebuildRequestedIntegrationEventHandler : IIntegrationEventHandler<RebuildRequestedIntegrationEvent>
{
    private readonly IUserRepository _users;
    private readonly IEventPublisher _publisher;
    private readonly IClock _clock;

    public RebuildRequestedIntegrationEventHandler(IUserRepository users, IEventPublisher publisher, IClock clock)
    {
        _users = users;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task HandleAsync(RebuildRequestedIntegrationEvent @event, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(@event.UserId);
        if (user is null)
            return;

        await _publisher.PublishAsync(new ProfileRecordedIntegrationEvent(user.Id, user.Name, user.Sex.ToString(), _clock.UtcNow), ct);
    }
}
