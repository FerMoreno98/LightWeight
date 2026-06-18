using LightWeight.Auth.Domain.Events;
using LightWeight.shared.Contracts.Auth;
using LightWeight.shared.Messaging;

namespace LightWeight.Auth.Application.Events;

public sealed class UserCreatedDomainEventHandler : IDomainEventHandler<UserCreatedDomainEvent>
{
    private readonly IEventPublisher _publisher;

    public UserCreatedDomainEventHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public async Task HandleAsync(UserCreatedDomainEvent @event, CancellationToken ct)
    {
        await _publisher.PublishAsync(
            new UserCreatedIntegrationEvent(@event.UserId, @event.Email, @event.OccurredAtUtc), ct);
    }
}