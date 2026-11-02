using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LightWeight.Dashboard.UnitTests.Application;

public class EventPublisherTests
{
    private static readonly RebuildRequestedIntegrationEvent Event = new(Guid.NewGuid(), DateTime.UnixEpoch);

    private readonly IIntegrationEventHandler<RebuildRequestedIntegrationEvent> _first =
        Substitute.For<IIntegrationEventHandler<RebuildRequestedIntegrationEvent>>();
    private readonly IIntegrationEventHandler<RebuildRequestedIntegrationEvent> _second =
        Substitute.For<IIntegrationEventHandler<RebuildRequestedIntegrationEvent>>();

    private EventPublisher Publisher()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(_first)
            .AddSingleton(_second)
            .BuildServiceProvider();
        return new EventPublisher(provider, NullLogger<EventPublisher>.Instance);
    }

    [Fact]
    public async Task A_failing_subscriber_does_not_fail_the_publish_and_the_next_one_still_runs()
    {
        _first.HandleAsync(Event, Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("projector failed"));

        await Publisher().PublishAsync(Event);

        await _second.Received(1).HandleAsync(Event, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_cancelled_request_stops_the_publish()
    {
        _first.HandleAsync(Event, Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => Publisher().PublishAsync(Event));

        await _second.DidNotReceive().HandleAsync(Event, Arg.Any<CancellationToken>());
    }
}
