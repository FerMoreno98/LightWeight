using FluentValidation;
using LightWeight.shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LightWeight.App.Exceptions;

/// <summary>
/// Turns every exception that reaches the host into a ProblemDetails response. Only the expected errors expose their
/// message: anything else answers a generic 500, so no internal details reach the client
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = ToProblemDetails(exception);
        int status = problem.Status!.Value;

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogInformation("{ExceptionType} on {Method} {Path}: {Status}", exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, status);

        httpContext.Response.StatusCode = status;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails ToProblemDetails(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request is not valid"
        },
        INotFoundException => Problem(StatusCodes.Status404NotFound, "Not found", exception.Message),
        IConflictException => Problem(StatusCodes.Status409Conflict, "Conflict", exception.Message),
        BusinessException => Problem(StatusCodes.Status400BadRequest, "The request could not be completed", exception.Message),
        // 403 and not 401: the frontend answers every 401 by refreshing the token, which would not help here
        UnauthorizedAccessException => Problem(StatusCodes.Status403Forbidden, "Forbidden", null),
        // The framework raises it for a body or parameter it cannot read
        BadHttpRequestException badRequest => Problem(badRequest.StatusCode, "The request is not valid", null),
        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected error", null)
    };

    private static ProblemDetails Problem(int status, string title, string? detail)
        => new() { Status = status, Title = title, Detail = detail };
}
