using LightWeight.BodyMetrics.Application.Commands.Perimeters;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.CorrectPerimeters;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.RegisterPerimeters;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.DeleteSkinFolds;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.RegisterSkinFolds;
using LightWeight.BodyMetrics.Application.Commands.Weights.CorrectWeight;
using LightWeight.BodyMetrics.Application.Commands.Weights.DeleteWeight;
using LightWeight.BodyMetrics.Application.Commands.Weights.RegisterWeight;
using LightWeight.BodyMetrics.Application.Exceptions;
using LightWeight.BodyMetrics.Application.Queries.SkinFolds.GetSkinFoldsHistory;
using LightWeight.BodyMetrics.Application.Queries.Weights.GetWeightHistory;
using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.BodyMetrics.Domain.Exceptions;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.Types;
using NSubstitute;

namespace LightWeight.BodyMetrics.UnitTests.Application;

public class MeasurementUseCasesTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 8, 0, 0, DateTimeKind.Utc);

    private readonly IWeightMeasurementRepository _weights = Substitute.For<IWeightMeasurementRepository>();
    private readonly IPerimetersRepository _perimeters = Substitute.For<IPerimetersRepository>();
    private readonly ISkinFoldsRepository _skinFolds = Substitute.For<ISkinFoldsRepository>();
    private readonly IBodyMetricsUnitOfWork _UOW = Substitute.For<IBodyMetricsUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly Guid _userId = Guid.CreateVersion7();

    public MeasurementUseCasesTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    private static PerimeterValues Waist(decimal waist) => new(null, null, null, null, null, null, null, waist, null, null, null, null, null, null);

    // ---------------------------------------------------------------- Weight

    [Fact]
    public async Task RegisterWeight_AddsTheMeasurementAndSaves()
    {
        // Arrange
        var handler = new RegisterWeightCommandHandler(_weights, _UOW, _clock);
        // Act
        Guid id = await handler.HandleAsync(new RegisterWeightCommand(_userId, 82.4m, Now.AddHours(-2)), default);
        // Assert
        await _weights.Received(1).AddAsync
        (
            Arg.Is<WeightMeasurement>(w => w.Id == id && w.UserId == _userId && w.Weight == 82.4m && w.MeasuredAt == Now.AddHours(-2)),
            Arg.Any<CancellationToken>()
        );
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterWeight_WithADateWithoutZone_TakesItAsUtc()
    {
        // Arrange
        var handler = new RegisterWeightCommandHandler(_weights, _UOW, _clock);
        var unspecified = new DateTime(2026, 10, 13, 7, 30, 0, DateTimeKind.Unspecified);
        // Act
        await handler.HandleAsync(new RegisterWeightCommand(_userId, 82.4m, unspecified), default);
        // Assert
        await _weights.Received(1).AddAsync
        (
            Arg.Is<WeightMeasurement>(w => w.MeasuredAt.Kind == DateTimeKind.Utc && w.MeasuredAt == new DateTime(2026, 10, 13, 7, 30, 0, DateTimeKind.Utc)),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task RegisterWeight_InTheFutureAccordingToTheClock_ThrowsDomainExceptionAndDoesNotSave()
    {
        // Arrange
        var handler = new RegisterWeightCommandHandler(_weights, _UOW, _clock);
        // Act
        // Assert
        await Assert.ThrowsAsync<MeasuredInTheFutureDomainException>
        (
            () => handler.HandleAsync(new RegisterWeightCommand(_userId, 82.4m, Now.AddDays(1)), default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void RegisterWeightValidator_ChecksUserAndDate(bool emptyUser, bool valid)
    {
        var command = new RegisterWeightCommand(emptyUser ? Guid.Empty : _userId, 82m, Now);
        Assert.Equal(valid, new RegisterWeightCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public async Task CorrectWeight_ChangesTheMeasurementAndSaves()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(_userId, 82.4m, Now, Now);
        _weights.GetByIdAsync(measurement.Id).Returns(measurement);
        var handler = new CorrectWeightCommandHandler(_weights, _UOW, _clock);
        // Act
        await handler.HandleAsync(new CorrectWeightCommand(measurement.Id, _userId, 81.9m, Now.AddDays(-1)), default);
        // Assert
        Assert.Equal(81.9m, measurement.Weight);
        Assert.Equal(Now.AddDays(-1), measurement.MeasuredAt);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CorrectWeight_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 82.4m, Now, Now);
        _weights.GetByIdAsync(measurement.Id).Returns(measurement);
        var handler = new CorrectWeightCommandHandler(_weights, _UOW, _clock);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => handler.HandleAsync(new CorrectWeightCommand(measurement.Id, _userId, 70m, Now), default)
        );
        Assert.Equal(82.4m, measurement.Weight);
    }

    [Fact]
    public async Task CorrectWeight_NotFound_ThrowsApplicationException()
    {
        // Arrange
        _weights.GetByIdAsync(Arg.Any<Guid>()).Returns((WeightMeasurement?)null);
        var handler = new CorrectWeightCommandHandler(_weights, _UOW, _clock);
        // Act
        // Assert
        await Assert.ThrowsAsync<MeasurementNotFoundApplicationException>
        (
            () => handler.HandleAsync(new CorrectWeightCommand(Guid.CreateVersion7(), _userId, 70m, Now), default)
        );
    }

    [Fact]
    public async Task DeleteWeight_RaisesTheEventRemovesAndSaves()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(_userId, 82.4m, Now, Now);
        measurement.ClearDomainEvents();
        _weights.GetByIdAsync(measurement.Id).Returns(measurement);
        var handler = new DeleteWeightCommandHandler(_weights, _UOW);
        // Act
        await handler.HandleAsync(new DeleteWeightCommand(measurement.Id, _userId), default);
        // Assert
        Assert.IsType<WeightDeletedDomainEvent>(Assert.Single(measurement.DomainEvents));
        _weights.Received(1).Remove(measurement);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteWeight_OfAnotherUser_ThrowsUnauthorizedAndDoesNotRemove()
    {
        // Arrange
        WeightMeasurement measurement = WeightMeasurement.Create(Guid.CreateVersion7(), 82.4m, Now, Now);
        _weights.GetByIdAsync(measurement.Id).Returns(measurement);
        var handler = new DeleteWeightCommandHandler(_weights, _UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => handler.HandleAsync(new DeleteWeightCommand(measurement.Id, _userId), default)
        );
        _weights.DidNotReceive().Remove(Arg.Any<WeightMeasurement>());
    }

    [Fact]
    public async Task GetWeightHistory_MapsTheMeasurementsOfTheUser()
    {
        // Arrange
        WeightMeasurement recent = WeightMeasurement.Create(_userId, 81m, Now, Now);
        WeightMeasurement older = WeightMeasurement.Create(_userId, 83m, Now.AddDays(-7), Now);
        _weights.GetAllOfAUserAsync(_userId).Returns(new List<WeightMeasurement> { recent, older });
        var handler = new GetWeightHistoryQueryHandler(_weights);
        // Act
        var result = await handler.HandleAsync(new GetWeightHistoryQuery(_userId), default);
        // Assert
        Assert.Equal(new[] { recent.Id, older.Id }, result.Select(r => r.Id));
        Assert.Equal(new[] { 81m, 83m }, result.Select(r => r.Weight));
        Assert.Equal(Now.AddDays(-7), result[1].MeasuredAt);
    }

    // ---------------------------------------------------------------- Perimeters

    [Fact]
    public async Task RegisterPerimeters_BuildsTheValueObjectFromTheValues()
    {
        // Arrange
        var handler = new RegisterPerimetersCommandHandler(_perimeters, _UOW, _clock);
        PerimeterMeasures expected = PerimeterMeasures.Create(waist: 82.1m);
        // Act
        await handler.HandleAsync(new RegisterPerimetersCommand(_userId, Waist(82.1m), Now), default);
        // Assert
        await _perimeters.Received(1).AddAsync
        (
            Arg.Is<Perimeters>(p => p.Measures == expected && p.UserId == _userId),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task RegisterPerimeters_WithoutAnyPerimeter_ThrowsDomainException()
    {
        // Arrange
        var handler = new RegisterPerimetersCommandHandler(_perimeters, _UOW, _clock);
        var empty = new PerimeterValues(null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        // Act
        // Assert
        await Assert.ThrowsAsync<NoPerimeterMeasuredDomainException>
        (
            () => handler.HandleAsync(new RegisterPerimetersCommand(_userId, empty, Now), default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CorrectPerimeters_ReplacesTheMeasures()
    {
        // Arrange
        Perimeters perimeters = Perimeters.Create(_userId, PerimeterMeasures.Create(waist: 82m, hip: 98m), Now, Now);
        _perimeters.GetByIdAsync(perimeters.Id).Returns(perimeters);
        var handler = new CorrectPerimetersCommandHandler(_perimeters, _UOW, _clock);
        // Act
        await handler.HandleAsync(new CorrectPerimetersCommand(perimeters.Id, _userId, Waist(81m), Now), default);
        // Assert
        Assert.Equal(81m, perimeters.Measures.Waist);
        Assert.Null(perimeters.Measures.Hip);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Skinfolds

    [Fact]
    public async Task RegisterSkinFolds_BuildsTheValueObjectFromTheValues()
    {
        // Arrange
        var handler = new RegisterSkinFoldsCommandHandler(_skinFolds, _UOW, _clock);
        // Act
        await handler.HandleAsync(new RegisterSkinFoldsCommand(_userId, new SkinFoldValues(15m, 12m, 9m, 10m, 8m, 6m), Now), default);
        // Assert
        await _skinFolds.Received(1).AddAsync
        (
            Arg.Is<SkinFolds>(s => s.Measures.Summatory == 60m && s.UserId == _userId),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DeleteSkinFolds_NotFound_ThrowsApplicationException()
    {
        // Arrange
        _skinFolds.GetByIdAsync(Arg.Any<Guid>()).Returns((SkinFolds?)null);
        var handler = new DeleteSkinFoldsCommandHandler(_skinFolds, _UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<MeasurementNotFoundApplicationException>
        (
            () => handler.HandleAsync(new DeleteSkinFoldsCommand(Guid.CreateVersion7(), _userId), default)
        );
    }

    [Fact]
    public async Task GetSkinFoldsHistory_ReturnsEverySkinFoldAndTheSummatory()
    {
        // Arrange
        SkinFolds complete = SkinFolds.Create(_userId, SkinFoldMeasures.Create(15m, 12m, 9m, 10m, 8m, 6m), Now, Now);
        SkinFolds partial = SkinFolds.Create(_userId, SkinFoldMeasures.Create(abdominal: 14m), Now.AddDays(-7), Now);
        _skinFolds.GetAllOfAUserAsync(_userId).Returns(new List<SkinFolds> { complete, partial });
        var handler = new GetSkinFoldsHistoryQueryHandler(_skinFolds);
        // Act
        var result = await handler.HandleAsync(new GetSkinFoldsHistoryQuery(_userId), default);
        // Assert
        Assert.Equal(60m, result[0].Summatory);
        Assert.Equal(15m, result[0].Abdominal);
        Assert.Equal(6m, result[0].Calf);
        Assert.Null(result[1].Summatory);
        Assert.Equal(14m, result[1].Abdominal);
    }
}
