using LightWeight.shared.Messaging;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Events;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Events;

/// <summary>Publishes the finished session, with all its sets, to the rest of the modules</summary>
public sealed class TrainingSessionDomainEventsHandler : IDomainEventHandler<TrainingSessionFinishedDomainEvent>
{
    private readonly ITrainingSessionRepository _sessions;
    private readonly IMacrocycleRepository _macrocycles;
    private readonly TrainingSessionEventFactory _factory;
    private readonly IEventPublisher _publisher;

    public TrainingSessionDomainEventsHandler
    (
        ITrainingSessionRepository sessions,
        IMacrocycleRepository macrocycles,
        TrainingSessionEventFactory factory,
        IEventPublisher publisher
    )
    {
        _sessions = sessions;
        _macrocycles = macrocycles;
        _factory = factory;
        _publisher = publisher;
    }

    public async Task HandleAsync(TrainingSessionFinishedDomainEvent @event, CancellationToken ct = default)
    {
        // Dispatched after saving: the session is read again with its sets, as it was stored
        TrainingSession? session = await _sessions.GetByIdAsync(@event.SessionId);
        Macrocycle? macrocycle = await _macrocycles.GetByMicrocycleIdAsync(@event.MicrocycleId);
        if (session is null || macrocycle is null) return;

        var events = await _factory.CreateAsync([session], [macrocycle], @event.OccurredAtUtc);
        foreach (var integrationEvent in events)
            await _publisher.PublishAsync(integrationEvent, ct);
    }
}
