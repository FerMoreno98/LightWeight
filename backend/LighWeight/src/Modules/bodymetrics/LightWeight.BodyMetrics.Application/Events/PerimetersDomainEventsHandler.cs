using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Messaging;

namespace LightWeight.BodyMetrics.Application.Events;

/// <summary>Publishes the perimeters domain events to the rest of the modules</summary>
public sealed class PerimetersDomainEventsHandler :
    IDomainEventHandler<PerimetersRegisteredDomainEvent>,
    IDomainEventHandler<PerimetersCorrectedDomainEvent>,
    IDomainEventHandler<PerimetersDeletedDomainEvent>
{
    private readonly IEventPublisher _publisher;

    public PerimetersDomainEventsHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task HandleAsync(PerimetersRegisteredDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(BodyMetricsIntegrationEventMapper.PerimetersRecorded(@event.MeasurementId, @event.UserId, @event.Measures, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(PerimetersCorrectedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(BodyMetricsIntegrationEventMapper.PerimetersRecorded(@event.MeasurementId, @event.UserId, @event.Measures, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(PerimetersDeletedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            new PerimetersDeletedIntegrationEvent(@event.MeasurementId, @event.UserId, @event.MeasuredAt, @event.OccurredAtUtc), ct);
}
