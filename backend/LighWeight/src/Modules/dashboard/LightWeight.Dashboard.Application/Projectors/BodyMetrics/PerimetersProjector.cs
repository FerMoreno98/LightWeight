using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Messaging;

namespace LightWeight.Dashboard.Application.Projectors.BodyMetrics;

/// <summary>Keeps the perimeters projection in sync with bodymetrics. "Recorded" always upserts</summary>
public sealed class PerimetersProjector :
    IIntegrationEventHandler<PerimetersRecordedIntegrationEvent>,
    IIntegrationEventHandler<PerimetersDeletedIntegrationEvent>
{
    private readonly IBodyMetricsProjectionStore _store;

    public PerimetersProjector(IBodyMetricsProjectionStore store)
        => _store = store;

    public Task HandleAsync(PerimetersRecordedIntegrationEvent @event, CancellationToken ct = default)
        => _store.UpsertPerimetersAsync(new PerimetersProjection
        {
            MeasurementId = @event.MeasurementId,
            UserId = @event.UserId,
            MeasuredAt = @event.MeasuredAt,
            Neck = @event.Neck,
            RightArmRelaxed = @event.RightArmRelaxed,
            RightArmTensioned = @event.RightArmTensioned,
            LeftArmRelaxed = @event.LeftArmRelaxed,
            LeftArmTensioned = @event.LeftArmTensioned,
            Chest = @event.Chest,
            Shoulder = @event.Shoulder,
            Waist = @event.Waist,
            Hip = @event.Hip,
            Abs = @event.Abs,
            RightThigh = @event.RightThigh,
            LeftThigh = @event.LeftThigh,
            RightCalf = @event.RightCalf,
            LeftCalf = @event.LeftCalf
        }, ct);

    public Task HandleAsync(PerimetersDeletedIntegrationEvent @event, CancellationToken ct = default)
        => _store.DeletePerimetersAsync(@event.MeasurementId, @event.UserId, ct);
}
