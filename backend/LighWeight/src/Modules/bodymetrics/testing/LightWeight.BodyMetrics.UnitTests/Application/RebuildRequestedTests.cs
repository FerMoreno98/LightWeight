using LightWeight.BodyMetrics.Application.Events;
using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using NSubstitute;

namespace LightWeight.BodyMetrics.UnitTests.Application;

public class RebuildRequestedTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MeasuredAt = new(2026, 10, 10, 7, 30, 0, DateTimeKind.Utc);

    private readonly IWeightMeasurementRepository _weights = Substitute.For<IWeightMeasurementRepository>();
    private readonly IPerimetersRepository _perimeters = Substitute.For<IPerimetersRepository>();
    private readonly ISkinFoldsRepository _skinFolds = Substitute.For<ISkinFoldsRepository>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly RebuildRequestedIntegrationEventHandler _handler;

    public RebuildRequestedTests()
    {
        _clock.UtcNow.Returns(Now);
        // Only the measurements of the requested user exist for the repositories: any other id returns nothing
        _weights.GetAllOfAUserAsync(Arg.Any<Guid>()).Returns([]);
        _perimeters.GetAllOfAUserAsync(Arg.Any<Guid>()).Returns([]);
        _skinFolds.GetAllOfAUserAsync(Arg.Any<Guid>()).Returns([]);
        _handler = new RebuildRequestedIntegrationEventHandler(_weights, _perimeters, _skinFolds, _publisher, _clock);
    }

    private RebuildRequestedIntegrationEvent Request() => new(_userId, Now);

    [Fact]
    public async Task PublishesOneWeightRecordedPerMeasurementOfTheUser()
    {
        // Arrange
        WeightMeasurement first = WeightMeasurement.Create(_userId, 82.4m, MeasuredAt, Now);
        WeightMeasurement second = WeightMeasurement.Create(_userId, 81.9m, MeasuredAt.AddDays(1), Now);
        _weights.GetAllOfAUserAsync(_userId).Returns([first, second]);
        // Act
        await _handler.HandleAsync(Request());
        // Assert
        await _publisher.Received(2).PublishAsync(Arg.Any<WeightRecordedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<WeightRecordedIntegrationEvent>(e =>
                e.MeasurementId == first.Id && e.UserId == _userId && e.Weight == 82.4m &&
                e.MeasuredAt == MeasuredAt && e.OccurredAtUtc == Now),
            Arg.Any<CancellationToken>()
        );
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<WeightRecordedIntegrationEvent>(e => e.MeasurementId == second.Id && e.Weight == 81.9m),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task PublishesEveryPerimeterOfTheMeasurements()
    {
        // Arrange
        PerimeterMeasures measures = PerimeterMeasures.Create
        (
            neck: 1m, rightArmRelaxed: 2m, rightArmTensioned: 3m, leftArmRelaxed: 4m, leftArmTensioned: 5m,
            chest: 6m, shoulder: 7m, waist: 8m, hip: 9m, abs: 10m,
            rightThigh: 11m, leftThigh: 12m, rightCalf: 13m, leftCalf: null
        );
        Perimeters perimeters = Perimeters.Create(_userId, measures, MeasuredAt, Now);
        _perimeters.GetAllOfAUserAsync(_userId).Returns([perimeters]);
        // Act
        await _handler.HandleAsync(Request());
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<PerimetersRecordedIntegrationEvent>(e =>
                e.MeasurementId == perimeters.Id && e.UserId == _userId && e.MeasuredAt == MeasuredAt &&
                e.Neck == 1m && e.RightArmRelaxed == 2m && e.RightArmTensioned == 3m &&
                e.LeftArmRelaxed == 4m && e.LeftArmTensioned == 5m && e.Chest == 6m &&
                e.Shoulder == 7m && e.Waist == 8m && e.Hip == 9m && e.Abs == 10m &&
                e.RightThigh == 11m && e.LeftThigh == 12m && e.RightCalf == 13m && e.LeftCalf == null &&
                e.OccurredAtUtc == Now),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task PublishesEverySkinFoldAndTheSummatoryOfTheMeasurements()
    {
        // Arrange
        SkinFoldMeasures measures = SkinFoldMeasures.Create(abdominal: 15m, suprailiac: 12m, tricipital: 9m, subscapular: 10m, thigh: 8m, calf: 6m);
        SkinFolds skinFolds = SkinFolds.Create(_userId, measures, MeasuredAt, Now);
        _skinFolds.GetAllOfAUserAsync(_userId).Returns([skinFolds]);
        // Act
        await _handler.HandleAsync(Request());
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<SkinFoldsRecordedIntegrationEvent>(e =>
                e.MeasurementId == skinFolds.Id && e.UserId == _userId && e.MeasuredAt == MeasuredAt &&
                e.Abdominal == 15m && e.Suprailiac == 12m && e.Tricipital == 9m &&
                e.Subscapular == 10m && e.Thigh == 8m && e.Calf == 6m &&
                e.Summatory == 60m && e.OccurredAtUtc == Now),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task OnlyReadsTheMeasurementsOfTheRequestedUser()
    {
        // Arrange
        Guid otherUser = Guid.CreateVersion7();
        _weights.GetAllOfAUserAsync(otherUser).Returns([WeightMeasurement.Create(otherUser, 70m, MeasuredAt, Now)]);
        // Act
        await _handler.HandleAsync(Request());
        // Assert
        await _weights.Received(1).GetAllOfAUserAsync(_userId);
        await _perimeters.Received(1).GetAllOfAUserAsync(_userId);
        await _skinFolds.Received(1).GetAllOfAUserAsync(_userId);
        await _weights.DidNotReceive().GetAllOfAUserAsync(otherUser);
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<WeightRecordedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithoutMeasurements_PublishesNothing()
    {
        // Act
        await _handler.HandleAsync(Request());
        // Assert
        Assert.Empty(_publisher.ReceivedCalls());
    }
}
