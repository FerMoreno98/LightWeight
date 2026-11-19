using LightWeight.Nutrition.Domain.Repositories;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;

namespace LightWeight.Nutrition.Application.Events;

/// <summary>
/// The dashboard has emptied the projections of a user: publish again a "Recorded" event for every day of the
/// diary of that user. Read-only: it does not modify days nor raise domain events
/// </summary>
public sealed class RebuildRequestedIntegrationEventHandler : IIntegrationEventHandler<RebuildRequestedIntegrationEvent>
{
    private readonly IDiaryDayRepository _days;
    private readonly IEventPublisher _publisher;
    private readonly IClock _clock;

    public RebuildRequestedIntegrationEventHandler(IDiaryDayRepository days, IEventPublisher publisher, IClock clock)
    {
        _days = days;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task HandleAsync(RebuildRequestedIntegrationEvent @event, CancellationToken ct = default)
    {
        DateTime now = _clock.UtcNow;
        foreach (var day in await _days.GetAllOfAUserAsync(@event.UserId, ct))
            await _publisher.PublishAsync(NutritionIntegrationEventMapper.DayRecorded(day, now), ct);
    }
}
