using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.Exceptions;
using LightWeight.BodyMetrics.Domain.ValueObjects;

namespace LightWeight.BodyMetrics.UnitTests.Domain;

public class PerimetersTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);

    private static PerimeterMeasures FullMeasures() => PerimeterMeasures.Create
    (
        neck: 38.5m,
        rightArmRelaxed: 33.2m,
        rightArmTensioned: 36.1m,
        leftArmRelaxed: 32.9m,
        leftArmTensioned: 35.8m,
        chest: 104.3m,
        shoulder: 120.4m,
        waist: 82.1m,
        hip: 98.7m,
        abs: 84m,
        rightThigh: 58.4m,
        leftThigh: 58.1m,
        rightCalf: 38.2m,
        leftCalf: 38m
    );

    // ---------------------------------------------------------------- PerimeterMeasures

    [Fact]
    public void Measures_WithEveryPerimeter_KeepsTheValues()
    {
        PerimeterMeasures measures = FullMeasures();
        Assert.Equal(38.5m, measures.Neck);
        Assert.Equal(82.1m, measures.Waist);
        Assert.Equal(38m, measures.LeftCalf);
    }

    [Fact]
    public void Measures_WithASinglePerimeter_IsValid()
    {
        PerimeterMeasures measures = PerimeterMeasures.Create(waist: 82m);
        Assert.Equal(82m, measures.Waist);
        Assert.Null(measures.Neck);
    }

    [Fact]
    public void Measures_WithoutAnyPerimeter_ThrowsDomainException()
    {
        Assert.Throws<NoPerimeterMeasuredDomainException>(() => PerimeterMeasures.Create());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-38.5)]
    [InlineData(300.1)]
    public void Measures_WithTheNeckOutOfRange_ThrowsDomainException(double neck)
    {
        Assert.Throws<MeasureOutOfRangeDomainException>(() => PerimeterMeasures.Create(neck: (decimal)neck, waist: 82m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(300.1)]
    public void Measures_WithAPerimeterOutOfRange_ThrowsDomainException(double waist)
    {
        Assert.Throws<MeasureOutOfRangeDomainException>(() => PerimeterMeasures.Create(waist: (decimal)waist));
    }

    [Fact]
    public void Measures_WithTheSameValues_AreEqual()
    {
        Assert.Equal(PerimeterMeasures.Create(waist: 82m, hip: 98m), PerimeterMeasures.Create(waist: 82m, hip: 98m));
        Assert.NotEqual(PerimeterMeasures.Create(waist: 82m), PerimeterMeasures.Create(waist: 83m));
    }

    // ---------------------------------------------------------------- Perimeters

    [Fact]
    public void Create_WithValidData_RegistersItAndRaisesTheEvent()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        PerimeterMeasures measures = FullMeasures();
        // Act
        Perimeters perimeters = Perimeters.Create(userId, measures, Now, Now);
        // Assert
        Assert.Equal(userId, perimeters.UserId);
        Assert.Equal(measures, perimeters.Measures);
        Assert.Equal(Now, perimeters.MeasuredAt);
        var @event = Assert.IsType<PerimetersRegisteredDomainEvent>(Assert.Single(perimeters.DomainEvents));
        Assert.Equal(perimeters.Id, @event.MeasurementId);
        Assert.Equal(userId, @event.UserId);
        Assert.Equal(measures, @event.Measures);
        Assert.Equal(Now, @event.MeasuredAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<UserIdEmptyDomainException>(() => Perimeters.Create(Guid.Empty, FullMeasures(), Now, Now));
    }

    [Fact]
    public void Create_MeasuredInTheFuture_ThrowsDomainException()
    {
        Assert.Throws<MeasuredInTheFutureDomainException>(() => Perimeters.Create(Guid.CreateVersion7(), FullMeasures(), Now.AddDays(1), Now));
    }

    [Fact]
    public void Correct_ReplacesTheMeasuresAndRaisesTheEvent()
    {
        // Arrange
        Perimeters perimeters = Perimeters.Create(Guid.CreateVersion7(), FullMeasures(), Now, Now);
        perimeters.ClearDomainEvents();
        PerimeterMeasures corrected = PerimeterMeasures.Create(waist: 81.5m);
        // Act
        perimeters.Correct(corrected, Now.AddHours(-2), Now);
        // Assert
        Assert.Equal(corrected, perimeters.Measures);
        Assert.Equal(Now.AddHours(-2), perimeters.MeasuredAt);
        var @event = Assert.IsType<PerimetersCorrectedDomainEvent>(Assert.Single(perimeters.DomainEvents));
        Assert.Equal(corrected, @event.Measures);
    }

    [Fact]
    public void Correct_MeasuredInTheFuture_ThrowsDomainExceptionAndKeepsTheMeasures()
    {
        // Arrange
        PerimeterMeasures original = FullMeasures();
        Perimeters perimeters = Perimeters.Create(Guid.CreateVersion7(), original, Now, Now);
        // Act
        // Assert
        Assert.Throws<MeasuredInTheFutureDomainException>(() => perimeters.Correct(PerimeterMeasures.Create(waist: 80m), Now.AddDays(1), Now));
        Assert.Equal(original, perimeters.Measures);
    }

    [Fact]
    public void Delete_RaisesTheEvent()
    {
        // Arrange
        Perimeters perimeters = Perimeters.Create(Guid.CreateVersion7(), FullMeasures(), Now, Now);
        perimeters.ClearDomainEvents();
        // Act
        perimeters.Delete();
        // Assert
        var @event = Assert.IsType<PerimetersDeletedDomainEvent>(Assert.Single(perimeters.DomainEvents));
        Assert.Equal(perimeters.Id, @event.MeasurementId);
    }
}
