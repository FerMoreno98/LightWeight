using System.Security.Claims;
using LightWeight.Training.Api.DTOs;
using LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class TrainingModule
{
    public static IEndpointRouteBuilder MapTrainingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training");

        group.MapPost("/macrocycle", CreateMacrocycle).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> CreateMacrocycle(
        CreateMacrocycleRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();

        await mediator.SendAsync(new CreateMacrocycleCommand(
            Guid.Parse(userId),
            request.StartAt,
            request.EndAt,
            request.TrainingStage,
            request.Periodization,
            request.Comments
        ), ct);

        return TypedResults.Ok();
    }
}
