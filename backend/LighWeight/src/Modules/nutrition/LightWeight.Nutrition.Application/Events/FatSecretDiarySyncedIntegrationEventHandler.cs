using LightWeight.Nutrition.Domain.Aggregates;
using LightWeight.Nutrition.Domain.Repositories;
using LightWeight.Nutrition.Domain.Services;
using LightWeight.Nutrition.Domain.Uow;
using LightWeight.Nutrition.Domain.ValueObjects;
using LightWeight.shared.Contracts.DeviceIntegration;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;

namespace LightWeight.Nutrition.Application.Events;

/// <summary>
/// DeviceIntegration has read the FatSecret diary of a user for a period: bring the recorded days of that
/// period in line with it (create, update or delete) and save. Saving dispatches the domain events, which
/// publish only the days that changed
/// </summary>
public sealed class FatSecretDiarySyncedIntegrationEventHandler : IIntegrationEventHandler<FatSecretDiarySyncedIntegrationEvent>
{
    private readonly IDiaryDayRepository _days;
    private readonly INutritionUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public FatSecretDiarySyncedIntegrationEventHandler(IDiaryDayRepository days, INutritionUnitOfWork unitOfWork, IClock clock)
    {
        _days = days;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task HandleAsync(FatSecretDiarySyncedIntegrationEvent @event, CancellationToken ct = default)
    {
        DiaryPeriod period = DiaryPeriod.Create(@event.From, @event.To);
        List<SyncedDay> synced = @event.Days
            .Select(d => new SyncedDay(d.Date, DailyIntake.Create(d.Calories, d.Protein, d.Carbohydrate, d.Fat)))
            .ToList();
        List<DiaryDay> recorded = await _days.GetBetweenAsync(@event.UserId, period.From, period.To, ct);

        DiarySynchronizationResult result = DiarySynchronization.Apply
        (
            @event.UserId, period, recorded, synced, UserCalendar.Today(_clock.UtcNow)
        );

        foreach (DiaryDay day in result.Created)
            await _days.AddAsync(day, ct);
        foreach (DiaryDay day in result.Deleted)
            _days.Remove(day);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
