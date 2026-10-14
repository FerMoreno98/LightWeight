namespace LightWeight.shared.Contracts.BodyMetrics;

// Registering and correcting a measurement publish the same "Recorded" event with the full values:
// consumers upsert by MeasurementId. Values only use primitive types so consumers don't depend on the
// bodymetrics domain. MeasuredAt is when the user took the measurement; OccurredAtUtc, when it was recorded

/// <summary>A body weight (kg) has been registered or corrected</summary>
public sealed record WeightRecordedIntegrationEvent(
    Guid     MeasurementId,
    Guid     UserId,
    decimal  Weight,
    DateTime MeasuredAt,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.BodyMetrics.WeightRecorded;
}

/// <summary>A body weight has been deleted</summary>
public sealed record WeightDeletedIntegrationEvent(
    Guid     MeasurementId,
    Guid     UserId,
    DateTime MeasuredAt,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.BodyMetrics.WeightDeleted;
}

/// <summary>A measurement of perimeters (cm) has been registered or corrected. Missing perimeters are null</summary>
public sealed record PerimetersRecordedIntegrationEvent(
    Guid     MeasurementId,
    Guid     UserId,
    DateTime MeasuredAt,
    decimal? Neck,
    decimal? RightArmRelaxed,
    decimal? RightArmTensioned,
    decimal? LeftArmRelaxed,
    decimal? LeftArmTensioned,
    decimal? Chest,
    decimal? Shoulder,
    decimal? Waist,
    decimal? Hip,
    decimal? Abs,
    decimal? RightThigh,
    decimal? LeftThigh,
    decimal? RightCalf,
    decimal? LeftCalf,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.BodyMetrics.PerimetersRecorded;
}

/// <summary>A measurement of perimeters has been deleted</summary>
public sealed record PerimetersDeletedIntegrationEvent(
    Guid     MeasurementId,
    Guid     UserId,
    DateTime MeasuredAt,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.BodyMetrics.PerimetersDeleted;
}

/// <summary>
/// A measurement of skinfolds (mm) has been registered or corrected. Missing skinfolds are null and
/// Summatory is only present when the six were measured. The fat percentage is not included: it needs the
/// user's sex, which belongs to user-profile
/// </summary>
public sealed record SkinFoldsRecordedIntegrationEvent(
    Guid     MeasurementId,
    Guid     UserId,
    DateTime MeasuredAt,
    decimal? Abdominal,
    decimal? Suprailiac,
    decimal? Tricipital,
    decimal? Subscapular,
    decimal? Thigh,
    decimal? Calf,
    decimal? Summatory,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.BodyMetrics.SkinFoldsRecorded;
}

/// <summary>A measurement of skinfolds has been deleted</summary>
public sealed record SkinFoldsDeletedIntegrationEvent(
    Guid     MeasurementId,
    Guid     UserId,
    DateTime MeasuredAt,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.BodyMetrics.SkinFoldsDeleted;
}
