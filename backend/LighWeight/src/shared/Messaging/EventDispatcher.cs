using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using LightWeight.shared.BuildingBlocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LightWeight.shared.Messaging;

/// <summary>
/// Default in-process domain event dispatcher.
/// Resolves all registered <see cref="IDomainEventHandler{T}"/> instances for each event's
/// concrete runtime type and invokes them sequentially in registration order.
/// </summary>
/// <remarks>
/// Register the dispatcher once per module and let modules add their own handlers:
/// <code>
/// services.AddScoped&lt;IEventDispatcher, EventDispatcher&gt;();
/// services.AddScoped&lt;IDomainEventHandler&lt;PredictionSubmitted&gt;, NotifyOnPredictionSubmitted&gt;();
/// </code>
/// Reflection is required here because <c>DispatchAsync</c> receives a list typed as
/// <see cref="IDomainEvent"/> — concrete event types are only known at runtime — but each
/// closed <c>HandleAsync</c> <see cref="MethodInfo"/> is cached after the first lookup.
/// </remarks>
public sealed class EventDispatcher : IEventDispatcher
{
    // Cache keyed by concrete event type — the cached MethodInfo is on the *closed* generic
    // interface IDomainEventHandler<TConcrete>, never on the open IDomainEventHandler<>.
    private static readonly ConcurrentDictionary<Type, MethodInfo> _handlerMethodCache = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(IServiceProvider serviceProvider, ILogger<EventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        IReadOnlyList<IDomainEvent> domainEvents,
        CancellationToken ct = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            Type concreteEventType = domainEvent.GetType();
            Type handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(concreteEventType);

            MethodInfo handleMethod = _handlerMethodCache.GetOrAdd(
                concreteEventType,
                _ => handlerInterface.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!);

            IEnumerable<object?> handlers = _serviceProvider.GetServices(handlerInterface);

            foreach (object? handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                // A failure is not logged here: it reaches the global exception handler with its whole trace
                long startedAt = Stopwatch.GetTimestamp();
                Task task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, ct })!;
                await task;
                _logger.LogDebug("Domain event {EventType} handled by {Handler} in {ElapsedMs} ms",
                    concreteEventType.Name, handler.GetType().Name, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            }
        }
    }
}
