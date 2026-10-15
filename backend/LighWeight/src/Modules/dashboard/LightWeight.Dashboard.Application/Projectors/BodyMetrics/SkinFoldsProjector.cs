using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Messaging;

namespace LightWeight.Dashboard.Application.Projectors.BodyMetrics;

/// <summary>Keeps the skinfolds projection in sync with bodymetrics. "Recorded" always upserts</summary>
public sealed class SkinFoldsProjector :
    IIntegrationEventHandler<SkinFoldsRecordedIntegrationEvent>,
    IIntegrationEventHandler<SkinFoldsDeletedIntegrationEvent>
{
    private readonly IBodyMetricsProjectionStore _store;

    public SkinFoldsProjector(IBodyMetricsProjectionStore store)
        => _store = store;

    public Task HandleAsync(SkinFoldsRecordedIntegrationEvent @event, CancellationToken ct = default)
        => _store.UpsertSkinFoldsAsync(new SkinFoldsProjection
        {
            MeasurementId = @event.MeasurementId,
            UserId = @event.UserId,
            MeasuredAt = @event.MeasuredAt,
            Abdominal = @event.Abdominal,
            Suprailiac = @event.Suprailiac,
            Tricipital = @event.Tricipital,
            Subscapular = @event.Subscapular,
            Thigh = @event.Thigh,
            Calf = @event.Calf,
            Summatory = @event.Summatory
        }, ct);

    public Task HandleAsync(SkinFoldsDeletedIntegrationEvent @event, CancellationToken ct = default)
        => _store.DeleteSkinFoldsAsync(@event.MeasurementId, @event.UserId, ct);
}
