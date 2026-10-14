using LightWeight.BodyMetrics.Application.Events;
using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.Contracts;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Messaging;
using NSubstitute;

namespace LightWeight.BodyMetrics.UnitTests.Application;

public class BodyMetricsEventsTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();

    // ---------------------------------------------------------------- Weight

    [Fact]
    public async Task WeightRegistered_PublishesWeightRecordedWithTheValues()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 82.4m, Now.AddHours(-1), Now);
        var @event = (WeightRegisteredDomainEvent)measurement.DomainEvents.Single();
        // Act
        await new WeightDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<WeightRecordedIntegrationEvent>(e =>
                e.MeasurementId == measurement.Id &&
                e.UserId == measurement.UserId &&
                e.Weight == 82.4m &&
                e.MeasuredAt == Now.AddHours(-1) &&
                e.OccurredAtUtc == @event.OccurredAtUtc &&
                e.EventName == EventNames.BodyMetrics.WeightRecorded),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task WeightCorrected_PublishesWeightRecordedWithTheNewValues()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 82.4m, Now, Now);
        measurement.ClearDomainEvents();
        measurement.Correct(81m, Now.AddDays(-1), Now);
        var @event = (WeightCorrectedDomainEvent)measurement.DomainEvents.Single();
        // Act
        await new WeightDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<WeightRecordedIntegrationEvent>(e => e.MeasurementId == measurement.Id && e.Weight == 81m && e.MeasuredAt == Now.AddDays(-1)),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task WeightDeleted_PublishesWeightDeleted()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 82.4m, Now, Now);
        measurement.ClearDomainEvents();
        measurement.Delete();
        var @event = (WeightDeletedDomainEvent)measurement.DomainEvents.Single();
        // Act
        await new WeightDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<WeightDeletedIntegrationEvent>(e =>
                e.MeasurementId == measurement.Id &&
                e.UserId == measurement.UserId &&
                e.EventName == EventNames.BodyMetrics.WeightDeleted),
            Arg.Any<CancellationToken>()
        );
    }

    // ---------------------------------------------------------------- Perimeters

    [Fact]
    public async Task PerimetersRegistered_PublishesEveryPerimeter()
    {
        // Arrange
        PerimeterMeasures measures = PerimeterMeasures.Create
        (
            neck: 1m, rightArmRelaxed: 2m, rightArmTensioned: 3m, leftArmRelaxed: 4m, leftArmTensioned: 5m,
            chest: 6m, shoulder: 7m, waist: 8m, hip: 9m, abs: 10m,
            rightThigh: 11m, leftThigh: 12m, rightCalf: 13m, leftCalf: null
        );
        Perimeters perimeters = Perimeters.Create(Guid.CreateVersion7(), measures, Now, Now);
        var @event = (PerimetersRegisteredDomainEvent)perimeters.DomainEvents.Single();
        // Act
        await new PerimetersDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<PerimetersRecordedIntegrationEvent>(e =>
                e.MeasurementId == perimeters.Id &&
                e.UserId == perimeters.UserId &&
                e.MeasuredAt == Now &&
                e.Neck == 1m && e.RightArmRelaxed == 2m && e.RightArmTensioned == 3m &&
                e.LeftArmRelaxed == 4m && e.LeftArmTensioned == 5m && e.Chest == 6m &&
                e.Shoulder == 7m && e.Waist == 8m && e.Hip == 9m && e.Abs == 10m &&
                e.RightThigh == 11m && e.LeftThigh == 12m && e.RightCalf == 13m && e.LeftCalf == null &&
                e.EventName == EventNames.BodyMetrics.PerimetersRecorded),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task PerimetersCorrected_PublishesPerimetersRecorded()
    {
        // Arrange
        Perimeters perimeters = Perimeters.Create(Guid.CreateVersion7(), PerimeterMeasures.Create(waist: 82m), Now, Now);
        perimeters.ClearDomainEvents();
        perimeters.Correct(PerimeterMeasures.Create(waist: 81m), Now, Now);
        var @event = (PerimetersCorrectedDomainEvent)perimeters.DomainEvents.Single();
        // Act
        await new PerimetersDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<PerimetersRecordedIntegrationEvent>(e => e.MeasurementId == perimeters.Id && e.Waist == 81m && e.Neck == null),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task PerimetersDeleted_PublishesPerimetersDeleted()
    {
        // Arrange
        Perimeters perimeters = Perimeters.Create(Guid.CreateVersion7(), PerimeterMeasures.Create(waist: 82m), Now, Now);
        perimeters.ClearDomainEvents();
        perimeters.Delete();
        var @event = (PerimetersDeletedDomainEvent)perimeters.DomainEvents.Single();
        // Act
        await new PerimetersDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<PerimetersDeletedIntegrationEvent>(e => e.MeasurementId == perimeters.Id && e.EventName == EventNames.BodyMetrics.PerimetersDeleted),
            Arg.Any<CancellationToken>()
        );
    }

    // ---------------------------------------------------------------- Skinfolds

    [Fact]
    public async Task SkinFoldsRegistered_PublishesEverySkinFoldAndTheSummatory()
    {
        // Arrange
        SkinFoldMeasures measures = SkinFoldMeasures.Create(abdominal: 15m, suprailiac: 12m, tricipital: 9m, subscapular: 10m, thigh: 8m, calf: 6m);
        SkinFolds skinFolds = SkinFolds.Create(Guid.CreateVersion7(), measures, Now, Now);
        var @event = (SkinFoldsRegisteredDomainEvent)skinFolds.DomainEvents.Single();
        // Act
        await new SkinFoldsDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<SkinFoldsRecordedIntegrationEvent>(e =>
                e.MeasurementId == skinFolds.Id &&
                e.UserId == skinFolds.UserId &&
                e.Abdominal == 15m && e.Suprailiac == 12m && e.Tricipital == 9m &&
                e.Subscapular == 10m && e.Thigh == 8m && e.Calf == 6m &&
                e.Summatory == 60m &&
                e.EventName == EventNames.BodyMetrics.SkinFoldsRecorded),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SkinFoldsCorrected_WithSomeSkinFoldMissing_PublishesWithoutSummatory()
    {
        // Arrange
        SkinFolds skinFolds = SkinFolds.Create(Guid.CreateVersion7(), SkinFoldMeasures.Create(abdominal: 15m), Now, Now);
        skinFolds.ClearDomainEvents();
        skinFolds.Correct(SkinFoldMeasures.Create(abdominal: 14m, calf: 6m), Now, Now);
        var @event = (SkinFoldsCorrectedDomainEvent)skinFolds.DomainEvents.Single();
        // Act
        await new SkinFoldsDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<SkinFoldsRecordedIntegrationEvent>(e =>
                e.MeasurementId == skinFolds.Id && e.Abdominal == 14m && e.Calf == 6m && e.Summatory == null),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SkinFoldsDeleted_PublishesSkinFoldsDeleted()
    {
        // Arrange
        SkinFolds skinFolds = SkinFolds.Create(Guid.CreateVersion7(), SkinFoldMeasures.Create(abdominal: 15m), Now, Now);
        skinFolds.ClearDomainEvents();
        skinFolds.Delete();
        var @event = (SkinFoldsDeletedDomainEvent)skinFolds.DomainEvents.Single();
        // Act
        await new SkinFoldsDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<SkinFoldsDeletedIntegrationEvent>(e => e.MeasurementId == skinFolds.Id && e.EventName == EventNames.BodyMetrics.SkinFoldsDeleted),
            Arg.Any<CancellationToken>()
        );
    }
}
