namespace LightWeight.shared.Contracts.DeviceIntegration;

/// <summary>Totals of one day of the FatSecret food diary: energy (kcal) and macronutrients (g)</summary>
public sealed record FatSecretDiaryDay(
    DateOnly Date,
    decimal  Calories,
    decimal  Protein,
    decimal  Carbohydrate,
    decimal  Fat);

/// <summary>
/// The FatSecret food diary of a user has been read for a period (From and To included, calendar days in
/// Europe/Madrid). Days lists only the days with entries: FatSecret omits the empty ones, so a day of the
/// period that is not in the list has no entries (the user may have emptied it)
/// </summary>
public sealed record FatSecretDiarySyncedIntegrationEvent(
    Guid                             UserId,
    DateOnly                         From,
    DateOnly                         To,
    IReadOnlyList<FatSecretDiaryDay> Days,
    DateTime                         OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.DeviceIntegration.FatSecretDiarySynced;
}
