namespace LightWeight.Dashboard.Application.Projections.BodyMetrics;

// Read models fed by the bodymetrics integration events. They are plain classes, not aggregates: the dashboard
// does not decide anything, it only keeps a copy of what happened in bodymetrics ready to be charted.
// The key is the MeasurementId of the source, so the same event can be applied any number of times

/// <summary>A body weight (kg) of a user</summary>
public sealed class WeightProjection
{
    public Guid MeasurementId { get; set; }
    public Guid UserId { get; set; }
    public DateTime MeasuredAt { get; set; }
    public decimal Weight { get; set; }
}

/// <summary>A measurement of perimeters (cm) of a user. Missing perimeters are null</summary>
public sealed class PerimetersProjection
{
    public Guid MeasurementId { get; set; }
    public Guid UserId { get; set; }
    public DateTime MeasuredAt { get; set; }
    public decimal? Neck { get; set; }
    public decimal? RightArmRelaxed { get; set; }
    public decimal? RightArmTensioned { get; set; }
    public decimal? LeftArmRelaxed { get; set; }
    public decimal? LeftArmTensioned { get; set; }
    public decimal? Chest { get; set; }
    public decimal? Shoulder { get; set; }
    public decimal? Waist { get; set; }
    public decimal? Hip { get; set; }
    public decimal? Abs { get; set; }
    public decimal? RightThigh { get; set; }
    public decimal? LeftThigh { get; set; }
    public decimal? RightCalf { get; set; }
    public decimal? LeftCalf { get; set; }
}

/// <summary>
/// A measurement of skinfolds (mm) of a user. Missing skinfolds are null and Summatory is only present when
/// the six were measured. It is stored (unlike in bodymetrics) because it is what the charts show
/// </summary>
public sealed class SkinFoldsProjection
{
    public Guid MeasurementId { get; set; }
    public Guid UserId { get; set; }
    public DateTime MeasuredAt { get; set; }
    public decimal? Abdominal { get; set; }
    public decimal? Suprailiac { get; set; }
    public decimal? Tricipital { get; set; }
    public decimal? Subscapular { get; set; }
    public decimal? Thigh { get; set; }
    public decimal? Calf { get; set; }
    public decimal? Summatory { get; set; }
}
