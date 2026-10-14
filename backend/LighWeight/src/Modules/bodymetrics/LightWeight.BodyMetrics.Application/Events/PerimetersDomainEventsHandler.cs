using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.ValueObjects;
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
        => _publisher.PublishAsync(Recorded(@event.MeasurementId, @event.UserId, @event.Measures, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(PerimetersCorrectedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(Recorded(@event.MeasurementId, @event.UserId, @event.Measures, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    public Task HandleAsync(PerimetersDeletedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            new PerimetersDeletedIntegrationEvent(@event.MeasurementId, @event.UserId, @event.MeasuredAt, @event.OccurredAtUtc), ct);

    /// <summary>The integration contract carries every perimeter as a primitive field</summary>
    private static PerimetersRecordedIntegrationEvent Recorded(Guid measurementId, Guid userId, PerimeterMeasures m, DateTime measuredAt, DateTime occurredAtUtc)
        => new(
            measurementId,
            userId,
            measuredAt,
            m.Neck,
            m.RightArmRelaxed,
            m.RightArmTensioned,
            m.LeftArmRelaxed,
            m.LeftArmTensioned,
            m.Chest,
            m.Shoulder,
            m.Waist,
            m.Hip,
            m.Abs,
            m.RightThigh,
            m.LeftThigh,
            m.RightCalf,
            m.LeftCalf,
            occurredAtUtc);
}
