namespace LightWeight.Dashboard.Api;

using System.Security.Claims;
using LightWeight.Dashboard.Application.Commands.RebuildProjections;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class DashboardModule
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        // Every endpoint works on the projections of the authenticated user
        var group = app.MapGroup("/api/dashboard").RequireAuthorization();

        group.MapPost("/rebuild", RebuildProjections);

        return app;
    }

    private static Guid UserId(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        return Guid.Parse(userId);
    }

    private static async Task<IResult> RebuildProjections(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new RebuildProjectionsCommand(UserId(httpContext)), ct);
        return TypedResults.NoContent();
    }
}
