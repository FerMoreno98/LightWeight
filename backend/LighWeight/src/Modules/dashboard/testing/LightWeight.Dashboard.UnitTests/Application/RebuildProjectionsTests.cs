using LightWeight.Dashboard.Application.Commands.RebuildProjections;
using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.shared.Contracts;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using NSubstitute;

namespace LightWeight.Dashboard.UnitTests.Application;

public class RebuildProjectionsTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 8, 0, 0, DateTimeKind.Utc);

    private readonly IBodyMetricsProjectionStore _store = Substitute.For<IBodyMetricsProjectionStore>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly Guid _userId = Guid.CreateVersion7();

    public RebuildProjectionsTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task EmptiesTheProjectionsOfTheUserBeforeAskingForTheEventsAgain()
    {
        // Arrange
        var handler = new RebuildProjectionsCommandHandler(_store, _publisher, _clock);
        // Act
        await handler.HandleAsync(new RebuildProjectionsCommand(_userId));
        // Assert: the order matters, emptying afterwards would delete what the sources publish
        Received.InOrder(() =>
        {
            _store.ClearUserAsync(_userId, Arg.Any<CancellationToken>());
            _publisher.PublishAsync
            (
                Arg.Is<RebuildRequestedIntegrationEvent>(e =>
                    e.UserId == _userId &&
                    e.OccurredAtUtc == Now &&
                    e.EventName == EventNames.Dashboard.RebuildRequested),
                Arg.Any<CancellationToken>()
            );
        });
    }

    [Fact]
    public async Task OnlyEmptiesTheRequestingUser()
    {
        // Arrange
        var handler = new RebuildProjectionsCommandHandler(_store, _publisher, _clock);
        // Act
        await handler.HandleAsync(new RebuildProjectionsCommand(_userId));
        // Assert
        await _store.Received(1).ClearUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _store.Received(1).ClearUserAsync(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_RejectsAnEmptyUser()
    {
        var validator = new RebuildProjectionsCommandValidator();

        Assert.False(validator.Validate(new RebuildProjectionsCommand(Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new RebuildProjectionsCommand(_userId)).IsValid);
    }
}
