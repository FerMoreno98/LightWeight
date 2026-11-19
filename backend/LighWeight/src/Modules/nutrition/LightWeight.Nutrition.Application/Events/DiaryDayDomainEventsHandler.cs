using LightWeight.Nutrition.Domain.Events;
using LightWeight.shared.Contracts.Nutrition;
using LightWeight.shared.Messaging;

namespace LightWeight.Nutrition.Application.Events;

/// <summary>Publishes the diary day domain events to the rest of the modules</summary>
public sealed class DiaryDayDomainEventsHandler :
    IDomainEventHandler<DiaryDayRecordedDomainEvent>,
    IDomainEventHandler<DiaryDayUpdatedDomainEvent>,
    IDomainEventHandler<DiaryDayDeletedDomainEvent>
{
    private readonly IEventPublisher _publisher;

    public DiaryDayDomainEventsHandler(IEventPublisher publisher)
        => _publisher = publisher;

    public Task HandleAsync(DiaryDayRecordedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            NutritionIntegrationEventMapper.DayRecorded(@event.DiaryDayId, @event.UserId, @event.Date, @event.Intake, @event.OccurredAtUtc), ct);

    public Task HandleAsync(DiaryDayUpdatedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            NutritionIntegrationEventMapper.DayRecorded(@event.DiaryDayId, @event.UserId, @event.Date, @event.Intake, @event.OccurredAtUtc), ct);

    public Task HandleAsync(DiaryDayDeletedDomainEvent @event, CancellationToken ct = default)
        => _publisher.PublishAsync(
            new DiaryDayDeletedIntegrationEvent(@event.DiaryDayId, @event.UserId, @event.Date, @event.OccurredAtUtc), ct);
}
