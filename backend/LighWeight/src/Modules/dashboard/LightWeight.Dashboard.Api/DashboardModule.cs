namespace LightWeight.Dashboard.Api;

using System.Security.Claims;
using LightWeight.Dashboard.Application.Commands.RebuildProjections;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetPerimetersEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetSkinFoldsEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetWeightEvolution;
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

        // Whole history for now; a range (?from=&to=, e.g. a macrocycle) will be added to these same routes
        group.MapGet("/bodymetrics/weight", GetWeightEvolution);
        group.MapGet("/bodymetrics/perimeters", GetPerimetersEvolution);
        group.MapGet("/bodymetrics/skinfolds", GetSkinFoldsEvolution);

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

    // ---------------------------------------------------------------- BodyMetrics

    private static async Task<IResult> GetWeightEvolution(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var evolution = await mediator.QueryAsync<GetWeightEvolutionQuery, WeightEvolutionResponse>(
            new GetWeightEvolutionQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(evolution);
    }

    private static async Task<IResult> GetPerimetersEvolution(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var evolution = await mediator.QueryAsync<GetPerimetersEvolutionQuery, List<PerimetersPoint>>(
            new GetPerimetersEvolutionQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(evolution);
    }

    private static async Task<IResult> GetSkinFoldsEvolution(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var evolution = await mediator.QueryAsync<GetSkinFoldsEvolutionQuery, List<SkinFoldsPoint>>(
            new GetSkinFoldsEvolutionQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(evolution);
    }
}
