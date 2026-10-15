using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Messaging;

namespace LightWeight.BodyMetrics.Application.Events;

/// <summary>Publishes the weight domain events to the rest of the modules</summary>
public sealed class WeightDomainEventsHandler :
    IDomainEventHandler<WeightRegisteredDomainEvent>,
    IDomainEventHandler<WeightCorrectedDomainEvent>,
    IDomainEventHandler<WeightDeletedDomainEvent>
{
    private readonly IEventPublisher _publisher;

    public WeightDomainEventsHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task HandleAsync(WeightRegisteredDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            BodyMetricsIntegrationEventMapper.WeightRecorded(@event.MeasurementId, @event.UserId, @event.Weight, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(WeightCorrectedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            BodyMetricsIntegrationEventMapper.WeightRecorded(@event.MeasurementId, @event.UserId, @event.Weight, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(WeightDeletedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            new WeightDeletedIntegrationEvent(@event.MeasurementId, @event.UserId, @event.MeasuredAt, @event.OccurredAtUtc), ct);
}
