using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.Contracts.BodyMetrics;

namespace LightWeight.BodyMetrics.Application.Events;

/// <summary>
/// Builds the "Recorded" integration events, flattening the value objects into the primitive fields of the contract.
/// Used when a measurement is registered or corrected (from the domain events) and when the dashboard asks to
/// publish the current state again (from the aggregates)
/// </summary>
internal static class BodyMetricsIntegrationEventMapper
{
    public static WeightRecordedIntegrationEvent WeightRecorded(Guid measurementId, Guid userId, decimal weight, DateTime measuredAt, DateTime occurredAtUtc)
        => new(measurementId, userId, weight, measuredAt, occurredAtUtc);

    public static WeightRecordedIntegrationEvent WeightRecorded(WeightMeasurement measurement, DateTime occurredAtUtc)
        => WeightRecorded(measurement.Id, measurement.UserId, measurement.Weight, measurement.MeasuredAt, occurredAtUtc);

    public static PerimetersRecordedIntegrationEvent PerimetersRecorded(Guid measurementId, Guid userId, PerimeterMeasures m, DateTime measuredAt, DateTime occurredAtUtc)
        => new(
            measurementId,
            userId,
            measuredAt,
            m.Neck,
            m.RightArmRelaxed,
            m.RightArmTensioned,
            m.LeftArmRelaxed,
            m.LeftArmTensioned,
            m.Chest,
            m.Shoulder,
            m.Waist,
            m.Hip,
            m.Abs,
            m.RightThigh,
            m.LeftThigh,
            m.RightCalf,
            m.LeftCalf,
            occurredAtUtc);

    public static PerimetersRecordedIntegrationEvent PerimetersRecorded(Perimeters perimeters, DateTime occurredAtUtc)
        => PerimetersRecorded(perimeters.Id, perimeters.UserId, perimeters.Measures, perimeters.MeasuredAt, occurredAtUtc);

    public static SkinFoldsRecordedIntegrationEvent SkinFoldsRecorded(Guid measurementId, Guid userId, SkinFoldMeasures m, DateTime measuredAt, DateTime occurredAtUtc)
        => new(
            measurementId,
            userId,
            measuredAt,
            m.Abdominal,
            m.Suprailiac,
            m.Tricipital,
            m.Subscapular,
            m.Thigh,
            m.Calf,
            m.Summatory,
            occurredAtUtc);

    public static SkinFoldsRecordedIntegrationEvent SkinFoldsRecorded(SkinFolds skinFolds, DateTime occurredAtUtc)
        => SkinFoldsRecorded(skinFolds.Id, skinFolds.UserId, skinFolds.Measures, skinFolds.MeasuredAt, occurredAtUtc);
}
