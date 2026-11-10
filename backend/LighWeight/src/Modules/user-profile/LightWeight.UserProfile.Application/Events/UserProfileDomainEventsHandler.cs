using LightWeight.shared.Contracts.UserProfile;
using LightWeight.shared.Messaging;
using LightWeight.UserProfile.Domain.Events;

namespace LightWeight.UserProfile.Application.Events;

/// <summary>Publishes the profile to the rest of the modules when it is completed or modified</summary>
public sealed class UserProfileDomainEventsHandler :
    IDomainEventHandler<UserCompletedDomainEvent>,
    IDomainEventHandler<UserProfileModifiedDomainEvent>
{
    private readonly IEventPublisher _publisher;

    public UserProfileDomainEventsHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task HandleAsync(UserCompletedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(new ProfileRecordedIntegrationEvent(@event.UserId, @event.Sex.ToString(), @event.OccurredAtUtc), ct);

    public Task HandleAsync(UserProfileModifiedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(new ProfileRecordedIntegrationEvent(@event.UserId, @event.Sex.ToString(), @event.OccurredAtUtc), ct);
}
