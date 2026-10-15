using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Messaging;

namespace LightWeight.BodyMetrics.Application.Events;

/// <summary>Publishes the skinfolds domain events to the rest of the modules</summary>
public sealed class SkinFoldsDomainEventsHandler :
    IDomainEventHandler<SkinFoldsRegisteredDomainEvent>,
    IDomainEventHandler<SkinFoldsCorrectedDomainEvent>,
    IDomainEventHandler<SkinFoldsDeletedDomainEvent>
{
    private readonly IEventPublisher _publisher;

    public SkinFoldsDomainEventsHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task HandleAsync(SkinFoldsRegisteredDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(BodyMetricsIntegrationEventMapper.SkinFoldsRecorded(@event.MeasurementId, @event.UserId, @event.Measures, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(SkinFoldsCorrectedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(BodyMetricsIntegrationEventMapper.SkinFoldsRecorded(@event.MeasurementId, @event.UserId, @event.Measures, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(SkinFoldsDeletedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            new SkinFoldsDeletedIntegrationEvent(@event.MeasurementId, @event.UserId, @event.MeasuredAt, @event.OccurredAtUtc), ct);
}
