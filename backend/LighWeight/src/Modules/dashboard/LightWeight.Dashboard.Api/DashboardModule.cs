namespace LightWeight.Dashboard.Api;

using System.Security.Claims;
using LightWeight.Dashboard.Application.Commands.RebuildProjections;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetPerimetersEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetSkinFoldsEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetWeightEvolution;
using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Queries.Training.GetExerciseProgress;
using LightWeight.Dashboard.Application.Queries.Training.GetExercisesProgress;
using LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;
using LightWeight.Dashboard.Application.Queries.Training.GetTrainingVolume;
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

        // Training: volume and session performance (home) and progress per exercise (Progreso)
        group.MapGet("/training/volume", GetTrainingVolume);
        group.MapGet("/training/sessions", GetSessionsPerformance);
        group.MapGet("/training/exercises", GetExercisesProgress);
        group.MapGet("/training/exercises/{exerciseId:guid}", GetExerciseProgress);

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

    // ---------------------------------------------------------------- Training

    private static async Task<IResult> GetTrainingVolume(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var volume = await mediator.QueryAsync<GetTrainingVolumeQuery, List<WeeklyVolumePoint>>(
            new GetTrainingVolumeQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(volume);
    }

    private static async Task<IResult> GetSessionsPerformance(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var sessions = await mediator.QueryAsync<GetSessionsPerformanceQuery, List<SessionPerformanceResponse>>(
            new GetSessionsPerformanceQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(sessions);
    }

    private static async Task<IResult> GetExercisesProgress(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var exercises = await mediator.QueryAsync<GetExercisesProgressQuery, List<ExerciseProgressSummary>>(
            new GetExercisesProgressQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(exercises);
    }

    private static async Task<IResult> GetExerciseProgress(Guid exerciseId, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var progress = await mediator.QueryAsync<GetExerciseProgressQuery, ExerciseProgressResponse?>(
            new GetExerciseProgressQuery(UserId(httpContext), exerciseId), ct);
        // Not performed by this user (or not existing): the same answer, so no ids of other users can be probed
        return progress is null ? TypedResults.NotFound() : TypedResults.Ok(progress);
    }
}
