using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Enum;
using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.Exceptions;
using LightWeight.BodyMetrics.Domain.ValueObjects;

namespace LightWeight.BodyMetrics.UnitTests.Domain;

public class SkinFoldsTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>Six skinfolds adding up to 60 mm</summary>
    private static SkinFoldMeasures FullMeasures() => SkinFoldMeasures.Create
    (
        abdominal: 15m,
        suprailiac: 12m,
        tricipital: 9m,
        subscapular: 10m,
        thigh: 8m,
        calf: 6m
    );

    // ---------------------------------------------------------------- SkinFoldMeasures

    [Fact]
    public void Measures_WithTheSixSkinFolds_HaveASummatory()
    {
        Assert.Equal(60m, FullMeasures().Summatory);
    }

    [Fact]
    public void Measures_WithSomeSkinFoldMissing_HaveNoSummatory()
    {
        SkinFoldMeasures measures = SkinFoldMeasures.Create(abdominal: 15m, suprailiac: 12m);
        Assert.Null(measures.Summatory);
        Assert.Equal(15m, measures.Abdominal);
    }

    [Fact]
    public void Measures_WithoutAnySkinFold_ThrowsDomainException()
    {
        Assert.Throws<NoSkinFoldMeasuredDomainException>(() => SkinFoldMeasures.Create());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.1)]
    public void Measures_WithASkinFoldOutOfRange_ThrowsDomainException(double calf)
    {
        Assert.Throws<MeasureOutOfRangeDomainException>(() => SkinFoldMeasures.Create(abdominal: 15m, calf: (decimal)calf));
    }

    // ---------------------------------------------------------------- SkinFolds

    [Fact]
    public void Create_WithValidData_RegistersItAndRaisesTheEvent()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        SkinFoldMeasures measures = FullMeasures();
        // Act
        SkinFolds skinFolds = SkinFolds.Create(userId, measures, Now, Now);
        // Assert
        Assert.Equal(userId, skinFolds.UserId);
        Assert.Equal(measures, skinFolds.Measures);
        var @event = Assert.IsType<SkinFoldsRegisteredDomainEvent>(Assert.Single(skinFolds.DomainEvents));
        Assert.Equal(skinFolds.Id, @event.MeasurementId);
        Assert.Equal(measures, @event.Measures);
        Assert.Equal(Now, @event.MeasuredAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<UserIdEmptyDomainException>(() => SkinFolds.Create(Guid.Empty, FullMeasures(), Now, Now));
    }

    [Fact]
    public void Create_MeasuredInTheFuture_ThrowsDomainException()
    {
        Assert.Throws<MeasuredInTheFutureDomainException>(() => SkinFolds.Create(Guid.CreateVersion7(), FullMeasures(), Now.AddHours(1), Now));
    }

    [Fact]
    public void Correct_ReplacesTheMeasuresAndRaisesTheEvent()
    {
        // Arrange
        SkinFolds skinFolds = SkinFolds.Create(Guid.CreateVersion7(), FullMeasures(), Now, Now);
        skinFolds.ClearDomainEvents();
        SkinFoldMeasures corrected = SkinFoldMeasures.Create(abdominal: 14m);
        // Act
        skinFolds.Correct(corrected, Now.AddDays(-1), Now);
        // Assert
        Assert.Equal(corrected, skinFolds.Measures);
        Assert.Equal(Now.AddDays(-1), skinFolds.MeasuredAt);
        var @event = Assert.IsType<SkinFoldsCorrectedDomainEvent>(Assert.Single(skinFolds.DomainEvents));
        Assert.Equal(corrected, @event.Measures);
    }

    [Fact]
    public void Delete_RaisesTheEvent()
    {
        // Arrange
        SkinFolds skinFolds = SkinFolds.Create(Guid.CreateVersion7(), FullMeasures(), Now, Now);
        skinFolds.ClearDomainEvents();
        // Act
        skinFolds.Delete();
        // Assert
        var @event = Assert.IsType<SkinFoldsDeletedDomainEvent>(Assert.Single(skinFolds.DomainEvents));
        Assert.Equal(skinFolds.Id, @event.MeasurementId);
    }

    // ---------------------------------------------------------------- Fat percentage

    [Theory]
    [InlineData(Sex.Male, 8.891)]     // 0.1051 * 60 + 2.585
    [InlineData(Sex.Female, 12.868)]  // 0.1548 * 60 + 3.580
    public void CalculateEstimatedFatPercentage_WithTheSixSkinFolds_UsesTheFormulaOfTheSex(Sex sex, double expected)
    {
        SkinFolds skinFolds = SkinFolds.Create(Guid.CreateVersion7(), FullMeasures(), Now, Now);
        Assert.Equal((decimal)expected, skinFolds.CalculateEstimatedFatPercentage(sex));
    }

    [Fact]
    public void CalculateEstimatedFatPercentage_WithoutTheSummatory_IsNull()
    {
        SkinFolds skinFolds = SkinFolds.Create(Guid.CreateVersion7(), SkinFoldMeasures.Create(abdominal: 15m), Now, Now);
        Assert.Null(skinFolds.CalculateEstimatedFatPercentage(Sex.Male));
    }
}
