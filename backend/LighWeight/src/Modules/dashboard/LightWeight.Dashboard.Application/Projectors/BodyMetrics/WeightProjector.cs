using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Messaging;

namespace LightWeight.Dashboard.Application.Projectors.BodyMetrics;

/// <summary>
/// Keeps the weight projection in sync with bodymetrics. "Recorded" arrives when a weight is registered, corrected
/// or published again during a rebuild, so it always upserts
/// </summary>
public sealed class WeightProjector :
    IIntegrationEventHandler<WeightRecordedIntegrationEvent>,
    IIntegrationEventHandler<WeightDeletedIntegrationEvent>
{
    private readonly IBodyMetricsProjectionStore _store;

    public WeightProjector(IBodyMetricsProjectionStore store)
        => _store = store;

    public Task HandleAsync(WeightRecordedIntegrationEvent @event, CancellationToken ct = default)
        => _store.UpsertWeightAsync(new WeightProjection
        {
            MeasurementId = @event.MeasurementId,
            UserId = @event.UserId,
            MeasuredAt = @event.MeasuredAt,
            Weight = @event.Weight
        }, ct);

    public Task HandleAsync(WeightDeletedIntegrationEvent @event, CancellationToken ct = default)
        => _store.DeleteWeightAsync(@event.MeasurementId, @event.UserId, ct);
}
