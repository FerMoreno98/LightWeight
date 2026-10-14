using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.Exceptions;

namespace LightWeight.BodyMetrics.UnitTests.Domain;

public class WeightMeasurementTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(20)]
    [InlineData(82.35)]
    [InlineData(400)]
    public void Create_WithValidWeight_RegistersItAndRaisesTheEvent(double weight)
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        DateTime measuredAt = Now.AddHours(-1);
        // Act
        WeightMeasurement measurement = WeightMeasurement.Create(userId, (decimal)weight, measuredAt, Now);
        // Assert
        Assert.Equal(userId, measurement.UserId);
        Assert.Equal((decimal)weight, measurement.Weight);
        Assert.Equal(measuredAt, measurement.MeasuredAt);
        var @event = Assert.IsType<WeightRegisteredDomainEvent>(Assert.Single(measurement.DomainEvents));
        Assert.Equal(measurement.Id, @event.MeasurementId);
        Assert.Equal(userId, @event.UserId);
        Assert.Equal((decimal)weight, @event.Weight);
        Assert.Equal(measuredAt, @event.MeasuredAt);
    }

    [Theory]
    [InlineData(19.9)]
    [InlineData(400.1)]
    [InlineData(0)]
    [InlineData(-80)]
    public void Create_WithWeightOutOfRange_ThrowsDomainException(double weight)
    {
        Assert.Throws<MeasureOutOfRangeDomainException>
        (
            () => WeightMeasurement.Create(Guid.CreateVersion7(), (decimal)weight, Now, Now)
        );
    }

    [Fact]
    public void Create_MeasuredInTheFuture_ThrowsDomainException()
    {
        Assert.Throws<MeasuredInTheFutureDomainException>
        (
            () => WeightMeasurement.Create(Guid.CreateVersion7(), 80m, Now.AddMinutes(6), Now)
        );
    }

    [Fact]
    public void Create_WithinTheClockTolerance_IsValid()
    {
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 80m, Now.AddMinutes(4), Now);
        Assert.Equal(Now.AddMinutes(4), measurement.MeasuredAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<UserIdEmptyDomainException>
        (
            () => WeightMeasurement.Create(Guid.Empty, 80m, Now, Now)
        );
    }

    [Fact]
    public void Correct_ChangesTheValuesAndRaisesTheEvent()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 80m, Now.AddDays(-1), Now);
        measurement.ClearDomainEvents();
        DateTime measuredAt = Now.AddDays(-2);
        // Act
        measurement.Correct(79.4m, measuredAt, Now);
        // Assert
        Assert.Equal(79.4m, measurement.Weight);
        Assert.Equal(measuredAt, measurement.MeasuredAt);
        var @event = Assert.IsType<WeightCorrectedDomainEvent>(Assert.Single(measurement.DomainEvents));
        Assert.Equal(measurement.Id, @event.MeasurementId);
        Assert.Equal(79.4m, @event.Weight);
        Assert.Equal(measuredAt, @event.MeasuredAt);
    }

    [Fact]
    public void Correct_WithWeightOutOfRange_ThrowsDomainExceptionAndKeepsTheValues()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 80m, Now, Now);
        // Act
        // Assert
        Assert.Throws<MeasureOutOfRangeDomainException>(() => measurement.Correct(800m, Now, Now));
        Assert.Equal(80m, measurement.Weight);
    }

    [Fact]
    public void Delete_RaisesTheEvent()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 80m, Now, Now);
        measurement.ClearDomainEvents();
        // Act
        measurement.Delete();
        // Assert
        var @event = Assert.IsType<WeightDeletedDomainEvent>(Assert.Single(measurement.DomainEvents));
        Assert.Equal(measurement.Id, @event.MeasurementId);
        Assert.Equal(measurement.UserId, @event.UserId);
        Assert.Equal(Now, @event.MeasuredAt);
    }
}
