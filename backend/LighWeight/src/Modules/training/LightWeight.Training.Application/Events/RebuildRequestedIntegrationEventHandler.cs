using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Events;

/// <summary>
/// The dashboard has emptied the projections of a user: publish again a "session finished" event for every finished
/// session of that user, from the current state of the tables. Events are not stored anywhere, so they are regenerated;
/// the dashboard cannot tell them from new ones and its projectors upsert them
/// </summary>
public sealed class RebuildRequestedIntegrationEventHandler : IIntegrationEventHandler<RebuildRequestedIntegrationEvent>
{
    private readonly ITrainingSessionRepository _sessions;
    private readonly IMacrocycleRepository _macrocycles;
    private readonly TrainingSessionEventFactory _factory;
    private readonly IEventPublisher _publisher;
    private readonly IClock _clock;

    public RebuildRequestedIntegrationEventHandler
    (
        ITrainingSessionRepository sessions,
        IMacrocycleRepository macrocycles,
        TrainingSessionEventFactory factory,
        IEventPublisher publisher,
        IClock clock
    )
    {
        _sessions = sessions;
        _macrocycles = macrocycles;
        _factory = factory;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task HandleAsync(RebuildRequestedIntegrationEvent @event, CancellationToken ct = default)
    {
        var sessions = await _sessions.GetFinishedOfAUserAsync(@event.UserId);
        if (sessions.Count == 0) return;
        var macrocycles = await _macrocycles.GetAllOfAUserAsync(@event.UserId);

        foreach (var integrationEvent in await _factory.CreateAsync(sessions, macrocycles, _clock.UtcNow))
            await _publisher.PublishAsync(integrationEvent, ct);
    }
}
