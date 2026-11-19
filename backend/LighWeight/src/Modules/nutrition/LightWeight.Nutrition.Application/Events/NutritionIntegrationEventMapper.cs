using LightWeight.Nutrition.Domain.Aggregates;
using LightWeight.Nutrition.Domain.ValueObjects;
using LightWeight.shared.Contracts.Nutrition;

namespace LightWeight.Nutrition.Application.Events;

/// <summary>
/// Builds the "Recorded" integration event, flattening the intake into the primitive fields of the contract.
/// Used when a day is created or updated (from the domain events) and on a rebuild (from the aggregates)
/// </summary>
internal static class NutritionIntegrationEventMapper
{
    public static DiaryDayRecordedIntegrationEvent DayRecorded(Guid diaryDayId, Guid userId, DateOnly date, DailyIntake intake, DateTime occurredAtUtc)
        => new(diaryDayId, userId, date, intake.Calories, intake.Protein, intake.Carbohydrate, intake.Fat, occurredAtUtc);

    public static DiaryDayRecordedIntegrationEvent DayRecorded(DiaryDay day, DateTime occurredAtUtc)
        => DayRecorded(day.Id, day.UserId, day.Date, day.Intake, occurredAtUtc);
}
