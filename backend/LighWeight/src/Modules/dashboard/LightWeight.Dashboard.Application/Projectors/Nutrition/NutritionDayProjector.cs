using LightWeight.Dashboard.Application.Projections.Nutrition;
using LightWeight.shared.Contracts.Nutrition;
using LightWeight.shared.Messaging;

namespace LightWeight.Dashboard.Application.Projectors.Nutrition;

/// <summary>"Recorded" (new day, changed totals or rebuild) always upserts; "Deleted" removes the day if it is there</summary>
public sealed class NutritionDayProjector :
    IIntegrationEventHandler<DiaryDayRecordedIntegrationEvent>,
    IIntegrationEventHandler<DiaryDayDeletedIntegrationEvent>
{
    private readonly INutritionProjectionStore _store;

    public NutritionDayProjector(INutritionProjectionStore store)
        => _store = store;

    public Task HandleAsync(DiaryDayRecordedIntegrationEvent @event, CancellationToken ct = default)
        => _store.UpsertDayAsync(new NutritionDayProjection
        {
            DiaryDayId = @event.DiaryDayId,
            UserId = @event.UserId,
            Date = @event.Date,
            Calories = @event.Calories,
            Protein = @event.Protein,
            Carbohydrate = @event.Carbohydrate,
            Fat = @event.Fat
        }, ct);

    public Task HandleAsync(DiaryDayDeletedIntegrationEvent @event, CancellationToken ct = default)
        => _store.DeleteDayAsync(@event.DiaryDayId, @event.UserId, ct);
}
