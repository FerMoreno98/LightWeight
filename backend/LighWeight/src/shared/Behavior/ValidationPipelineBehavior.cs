using FluentValidation;
using LightWeight.shared.Mediator;

namespace LightWeight.shared.Behavior;

public sealed class ValidationPipelineBehavior<TRequest> : IPipelineBehavior<TRequest>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task HandleAsync(TRequest request, RequestHandlerDelegate next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        await next();
    }
}

public sealed class ValidationPipelineBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : ICommand<TResult>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();

        var context  = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}