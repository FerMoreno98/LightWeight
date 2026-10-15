using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;

namespace LightWeight.Dashboard.Application.Commands.RebuildProjections;

public sealed class RebuildProjectionsCommandHandler : ICommandHandler<RebuildProjectionsCommand>
{
    private readonly IBodyMetricsProjectionStore _bodyMetrics;
    private readonly IEventPublisher _publisher;
    private readonly IClock _clock;

    public RebuildProjectionsCommandHandler(IBodyMetricsProjectionStore bodyMetrics, IEventPublisher publisher, IClock clock)
    {
        _bodyMetrics = bodyMetrics;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task HandleAsync(RebuildProjectionsCommand command, CancellationToken ct = default)
    {
        // Emptying first also removes the measurements whose "Deleted" event the dashboard missed.
        // The dashboard does not know which modules answer: each source subscribed to the event publishes
        // a "Recorded" for every row of the user, and the projectors fill the tables again
        await _bodyMetrics.ClearUserAsync(command.UserId, ct);
        await _publisher.PublishAsync(new RebuildRequestedIntegrationEvent(command.UserId, _clock.UtcNow), ct);
    }
}
