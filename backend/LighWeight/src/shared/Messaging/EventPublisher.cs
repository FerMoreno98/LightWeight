using System.Diagnostics;
using LightWeight.shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LightWeight.shared.Messaging;

/// <summary>
/// Default in-process integration event publisher.
/// Resolves all registered <see cref="IIntegrationEventHandler{T}"/> instances from the
/// <see cref="IServiceProvider"/> at publish time and invokes them sequentially in
/// registration order.
/// </summary>
/// <remarks>
/// Register the publisher once per module and let modules add their own handlers:
/// <code>
/// services.AddScoped&lt;IEventPublisher, EventPublisher&gt;();
/// services.AddScoped&lt;IIntegrationEventHandler&lt;PredictionSubmittedV1&gt;, NotifyOnPredictionSubmitted&gt;();
/// </code>
/// Zero registered handlers is a valid state in a monolith — an integration event with no
/// in-process subscribers is published as a no-op.
/// The publisher runs after the commit of the source module, so a failing subscriber is logged and
/// the next one still runs: the data of the user is saved and must not be answered as an error.
/// Only the event name and the handler are logged, never the payload, which carries personal data.
/// </remarks>
public sealed class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T @event, CancellationToken ct = default)
        where T : IIntegrationEvent
    {
        IEnumerable<IIntegrationEventHandler<T>> handlers =
            _serviceProvider.GetServices<IIntegrationEventHandler<T>>();

        foreach (IIntegrationEventHandler<T> handler in handlers)
        {
            long startedAt = Stopwatch.GetTimestamp();
            try
            {
                await handler.HandleAsync(@event, ct);
                _logger.LogDebug("Integration event {EventName} handled by {Handler} in {ElapsedMs} ms",
                    @event.EventName, handler.GetType().Name, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Integration event {EventName} failed in {Handler}",
                    @event.EventName, handler.GetType().Name);
            }
        }
    }
}
