using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Application.Projectors.BodyMetrics;
using LightWeight.Dashboard.UnitTests.Fakes;
using LightWeight.shared.Contracts.BodyMetrics;

namespace LightWeight.Dashboard.UnitTests.Application;

public class BodyMetricsProjectorsTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MeasuredAt = new(2026, 10, 10, 7, 30, 0, DateTimeKind.Utc);

    private readonly InMemoryBodyMetricsProjectionStore _store = new();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _measurementId = Guid.CreateVersion7();

    // ---------------------------------------------------------------- Weight

    [Fact]
    public async Task WeightRecorded_StoresEveryField()
    {
        // Act
        await new WeightProjector(_store).HandleAsync(new WeightRecordedIntegrationEvent(_measurementId, _userId, 82.4m, MeasuredAt, Now));
        // Assert
        WeightProjection row = Assert.Single(_store.Weights.Values);
        Assert.Equal(_measurementId, row.MeasurementId);
        Assert.Equal(_userId, row.UserId);
        Assert.Equal(MeasuredAt, row.MeasuredAt);
        Assert.Equal(82.4m, row.Weight);
    }

    [Fact]
    public async Task WeightRecordedAgain_OverwritesTheRowInsteadOfDuplicatingIt()
    {
        // Arrange
        var projector = new WeightProjector(_store);
        await projector.HandleAsync(new WeightRecordedIntegrationEvent(_measurementId, _userId, 82.4m, MeasuredAt, Now));
        // Act: a correction arrives as another "Recorded" with the same MeasurementId
        await projector.HandleAsync(new WeightRecordedIntegrationEvent(_measurementId, _userId, 81m, MeasuredAt.AddDays(-1), Now));
        // Assert
        WeightProjection row = Assert.Single(_store.Weights.Values);
        Assert.Equal(81m, row.Weight);
        Assert.Equal(MeasuredAt.AddDays(-1), row.MeasuredAt);
    }

    [Fact]
    public async Task WeightRecordedTwiceWithTheSameValues_LeavesTheSameResult()
    {
        // Arrange
        var projector = new WeightProjector(_store);
        var @event = new WeightRecordedIntegrationEvent(_measurementId, _userId, 82.4m, MeasuredAt, Now);
        // Act
        await projector.HandleAsync(@event);
        await projector.HandleAsync(@event);
        // Assert
        WeightProjection row = Assert.Single(_store.Weights.Values);
        Assert.Equal(82.4m, row.Weight);
    }

    [Fact]
    public async Task WeightDeleted_RemovesTheRow_AndDeletingItAgainDoesNothing()
    {
        // Arrange
        var projector = new WeightProjector(_store);
        await projector.HandleAsync(new WeightRecordedIntegrationEvent(_measurementId, _userId, 82.4m, MeasuredAt, Now));
        var deleted = new WeightDeletedIntegrationEvent(_measurementId, _userId, MeasuredAt, Now);
        // Act
        await projector.HandleAsync(deleted);
        await projector.HandleAsync(deleted);
        // Assert
        Assert.Empty(_store.Weights);
    }

    // ---------------------------------------------------------------- Perimeters

    [Fact]
    public async Task PerimetersRecorded_StoresEveryPerimeter()
    {
        // Arrange
        var @event = new PerimetersRecordedIntegrationEvent
        (
            _measurementId, _userId, MeasuredAt,
            1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 9m, 10m, 11m, 12m, 13m, null,
            Now
        );
        // Act
        await new PerimetersProjector(_store).HandleAsync(@event);
        // Assert
        PerimetersProjection row = Assert.Single(_store.Perimeters.Values);
        Assert.Equal(_measurementId, row.MeasurementId);
        Assert.Equal(_userId, row.UserId);
        Assert.Equal(MeasuredAt, row.MeasuredAt);
        Assert.Equal
        (
            new decimal?[] { 1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 9m, 10m, 11m, 12m, 13m, null },
            new[]
            {
                row.Neck, row.RightArmRelaxed, row.RightArmTensioned, row.LeftArmRelaxed, row.LeftArmTensioned,
                row.Chest, row.Shoulder, row.Waist, row.Hip, row.Abs,
                row.RightThigh, row.LeftThigh, row.RightCalf, row.LeftCalf
            }
        );
    }

    [Fact]
    public async Task PerimetersRecordedAgain_OverwritesTheRow()
    {
        // Arrange
        var projector = new PerimetersProjector(_store);
        await projector.HandleAsync(Perimeters(waist: 82m, neck: 38m));
        // Act: the correction removes the neck
        await projector.HandleAsync(Perimeters(waist: 81m, neck: null));
        // Assert
        PerimetersProjection row = Assert.Single(_store.Perimeters.Values);
        Assert.Equal(81m, row.Waist);
        Assert.Null(row.Neck);
    }

    [Fact]
    public async Task PerimetersDeleted_RemovesTheRow_AndDeletingItAgainDoesNothing()
    {
        // Arrange
        var projector = new PerimetersProjector(_store);
        await projector.HandleAsync(Perimeters(waist: 82m, neck: null));
        var deleted = new PerimetersDeletedIntegrationEvent(_measurementId, _userId, MeasuredAt, Now);
        // Act
        await projector.HandleAsync(deleted);
        await projector.HandleAsync(deleted);
        // Assert
        Assert.Empty(_store.Perimeters);
    }

    private PerimetersRecordedIntegrationEvent Perimeters(decimal waist, decimal? neck)
        => new(_measurementId, _userId, MeasuredAt, neck, null, null, null, null, null, null, waist, null, null, null, null, null, null, Now);

    // ---------------------------------------------------------------- Skinfolds

    [Fact]
    public async Task SkinFoldsRecorded_StoresEverySkinFoldAndTheSummatory()
    {
        // Arrange
        var @event = new SkinFoldsRecordedIntegrationEvent(_measurementId, _userId, MeasuredAt, 15m, 12m, 9m, 10m, 8m, 6m, 60m, Now);
        // Act
        await new SkinFoldsProjector(_store).HandleAsync(@event);
        // Assert
        SkinFoldsProjection row = Assert.Single(_store.SkinFolds.Values);
        Assert.Equal(_measurementId, row.MeasurementId);
        Assert.Equal(_userId, row.UserId);
        Assert.Equal(MeasuredAt, row.MeasuredAt);
        Assert.Equal
        (
            new decimal?[] { 15m, 12m, 9m, 10m, 8m, 6m, 60m },
            new[] { row.Abdominal, row.Suprailiac, row.Tricipital, row.Subscapular, row.Thigh, row.Calf, row.Summatory }
        );
    }

    [Fact]
    public async Task SkinFoldsRecordedAgainWithAMissingSkinFold_OverwritesTheRowWithoutSummatory()
    {
        // Arrange
        var projector = new SkinFoldsProjector(_store);
        await projector.HandleAsync(new SkinFoldsRecordedIntegrationEvent(_measurementId, _userId, MeasuredAt, 15m, 12m, 9m, 10m, 8m, 6m, 60m, Now));
        // Act
        await projector.HandleAsync(new SkinFoldsRecordedIntegrationEvent(_measurementId, _userId, MeasuredAt, 14m, null, null, null, null, 6m, null, Now));
        // Assert
        SkinFoldsProjection row = Assert.Single(_store.SkinFolds.Values);
        Assert.Equal(14m, row.Abdominal);
        Assert.Null(row.Suprailiac);
        Assert.Null(row.Summatory);
    }

    [Fact]
    public async Task SkinFoldsDeleted_RemovesTheRow_AndDeletingItAgainDoesNothing()
    {
        // Arrange
        var projector = new SkinFoldsProjector(_store);
        await projector.HandleAsync(new SkinFoldsRecordedIntegrationEvent(_measurementId, _userId, MeasuredAt, 15m, null, null, null, null, null, null, Now));
        var deleted = new SkinFoldsDeletedIntegrationEvent(_measurementId, _userId, MeasuredAt, Now);
        // Act
        await projector.HandleAsync(deleted);
        await projector.HandleAsync(deleted);
        // Assert
        Assert.Empty(_store.SkinFolds);
    }

    // ---------------------------------------------------------------- Isolation

    [Fact]
    public async Task Deleted_WithTheIdOfAnotherUser_DoesNotRemoveTheRow()
    {
        // Arrange
        var projector = new WeightProjector(_store);
        await projector.HandleAsync(new WeightRecordedIntegrationEvent(_measurementId, _userId, 82.4m, MeasuredAt, Now));
        // Act
        await projector.HandleAsync(new WeightDeletedIntegrationEvent(_measurementId, Guid.CreateVersion7(), MeasuredAt, Now));
        // Assert
        Assert.Single(_store.Weights);
    }
}
