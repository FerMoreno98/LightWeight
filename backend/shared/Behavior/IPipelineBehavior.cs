using LightWeight.shared.Mediator;

namespace LightWeight.shared.Behavior;
public interface IPipelineBehavior<TRequest>
{
    Task HandleAsync(TRequest request, RequestHandlerDelegate next, CancellationToken ct);
}

public delegate Task RequestHandlerDelegate();

public delegate Task<TResult> RequestHandlerDelegate<TResult>();

public interface IPipelineBehavior<TRequest, TResult> where TRequest : ICommand<TResult>
{
    Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct);
}