using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;

namespace LightWeight.BodyMetrics.Application.Events;

/// <summary>
/// The dashboard has emptied the projections of a user: publish again a "Recorded" event for every measurement
/// of that user, from the current state of the tables. Events are not stored anywhere, so they are regenerated;
/// the dashboard cannot tell them from new ones and its projectors upsert them
/// </summary>
public sealed class RebuildRequestedIntegrationEventHandler : IIntegrationEventHandler<RebuildRequestedIntegrationEvent>
{
    private readonly IWeightMeasurementRepository _weights;
    private readonly IPerimetersRepository _perimeters;
    private readonly ISkinFoldsRepository _skinFolds;
    private readonly IEventPublisher _publisher;
    private readonly IClock _clock;

    public RebuildRequestedIntegrationEventHandler
    (
        IWeightMeasurementRepository weights,
        IPerimetersRepository perimeters,
        ISkinFoldsRepository skinFolds,
        IEventPublisher publisher,
        IClock clock
    )
    {
        _weights = weights;
        _perimeters = perimeters;
        _skinFolds = skinFolds;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task HandleAsync(RebuildRequestedIntegrationEvent @event, CancellationToken ct = default)
    {
        DateTime now = _clock.UtcNow;

        foreach (var weight in await _weights.GetAllOfAUserAsync(@event.UserId))
            await _publisher.PublishAsync(BodyMetricsIntegrationEventMapper.WeightRecorded(weight, now), ct);

        foreach (var perimeters in await _perimeters.GetAllOfAUserAsync(@event.UserId))
            await _publisher.PublishAsync(BodyMetricsIntegrationEventMapper.PerimetersRecorded(perimeters, now), ct);

        foreach (var skinFolds in await _skinFolds.GetAllOfAUserAsync(@event.UserId))
            await _publisher.PublishAsync(BodyMetricsIntegrationEventMapper.SkinFoldsRecorded(skinFolds, now), ct);
    }
}
