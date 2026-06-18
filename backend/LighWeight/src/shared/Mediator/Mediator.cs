using Microsoft.Extensions.DependencyInjection;
using LightWeight.shared.Behavior;

namespace LightWeight.shared.Mediator;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<TCommand>>()
            .ToList();

        async Task Handler()
        {
            var handler = ResolveHandler<ICommandHandler<TCommand>>(typeof(ICommandHandler<TCommand>));
            await handler.HandleAsync(command, ct);
        }

        var pipeline = behaviors
            .AsEnumerable()
            .Reverse()
            .Aggregate(
                (RequestHandlerDelegate)Handler,
                (next, behavior) => () => behavior.HandleAsync(command, next, ct));

        await pipeline();
    }

    /// <inheritdoc />
    public async Task<TResult> SendAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<TCommand, TResult>>()
            .ToList();

        async Task<TResult> Handler()
        {
            var handler = ResolveHandler<ICommandHandler<TCommand, TResult>>(
                typeof(ICommandHandler<TCommand, TResult>));
            return await handler.HandleAsync(command, ct);
        }

        var pipeline = behaviors
            .AsEnumerable()
            .Reverse()
            .Aggregate(
                (RequestHandlerDelegate<TResult>)Handler,
                (next, behavior) => () => behavior.HandleAsync(command, next, ct));

        return await pipeline();
    }

    /// <inheritdoc />
    public async Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken ct = default)
        where TQuery : IQuery<TResult>
    {
        var handler = ResolveHandler<IQueryHandler<TQuery, TResult>>(
            typeof(IQueryHandler<TQuery, TResult>));
        return await handler.HandleAsync(query, ct);
    }

    private THandler ResolveHandler<THandler>(Type handlerType)
    {
        object? handler = _serviceProvider.GetService(handlerType);
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for '{handlerType.FullName}'. " +
                "Ensure the handler is registered in the DI container.");
        }
        return (THandler)handler;
    }
}