namespace LightWeight.shared.Contracts.Nutrition;

// A day of the food diary is published with its full totals when it is created, when they change and on a
// rebuild: consumers upsert by DiaryDayId. Date is a calendar day of the user (Europe/Madrid), without time

/// <summary>The totals of a day of the food diary: energy (kcal) and macronutrients (g)</summary>
public sealed record DiaryDayRecordedIntegrationEvent(
    Guid     DiaryDayId,
    Guid     UserId,
    DateOnly Date,
    decimal  Calories,
    decimal  Protein,
    decimal  Carbohydrate,
    decimal  Fat,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.Nutrition.DayRecorded;
}

/// <summary>A day no longer has entries in the food diary</summary>
public sealed record DiaryDayDeletedIntegrationEvent(
    Guid     DiaryDayId,
    Guid     UserId,
    DateOnly Date,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.Nutrition.DayDeleted;
}
