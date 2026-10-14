namespace LightWeight.BodyMetrics.Api;

using System.Security.Claims;
using LightWeight.BodyMetrics.Api.DTOs;
using LightWeight.BodyMetrics.Application.Commands.Weights.RegisterWeight;
using LightWeight.BodyMetrics.Application.Commands.Weights.CorrectWeight;
using LightWeight.BodyMetrics.Application.Commands.Weights.DeleteWeight;
using LightWeight.BodyMetrics.Application.Queries.Weights.GetWeightHistory;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.RegisterPerimeters;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.CorrectPerimeters;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.DeletePerimeters;
using LightWeight.BodyMetrics.Application.Queries.Perimeters.GetPerimetersHistory;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.RegisterSkinFolds;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.CorrectSkinFolds;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.DeleteSkinFolds;
using LightWeight.BodyMetrics.Application.Queries.SkinFolds.GetSkinFoldsHistory;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

public static class BodyMetricsModule
{
    public static IEndpointRouteBuilder MapBodyMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        // Every endpoint works on the measurements of the authenticated user
        var group = app.MapGroup("/api/bodymetrics").RequireAuthorization();

        group.MapGet("/weights", GetWeightHistory);
        group.MapPost("/weights", RegisterWeight);
        group.MapPut("/weights/{measurementId:guid}", CorrectWeight);
        group.MapDelete("/weights/{measurementId:guid}", DeleteWeight);
        group.MapGet("/perimeters", GetPerimetersHistory);
        group.MapPost("/perimeters", RegisterPerimeters);
        group.MapPut("/perimeters/{measurementId:guid}", CorrectPerimeters);
        group.MapDelete("/perimeters/{measurementId:guid}", DeletePerimeters);
        group.MapGet("/skinfolds", GetSkinFoldsHistory);
        group.MapPost("/skinfolds", RegisterSkinFolds);
        group.MapPut("/skinfolds/{measurementId:guid}", CorrectSkinFolds);
        group.MapDelete("/skinfolds/{measurementId:guid}", DeleteSkinFolds);

        return app;
    }

    private static Guid UserId(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        return Guid.Parse(userId);
    }

    // ---------------------------------------------------------------- Weights

    private static async Task<IResult> GetWeightHistory(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var history = await mediator.QueryAsync<GetWeightHistoryQuery, List<WeightResponse>>(
            new GetWeightHistoryQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(history);
    }

    private static async Task<IResult> RegisterWeight(WeightRequest request, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var id = await mediator.SendAsync<RegisterWeightCommand, Guid>(
            new RegisterWeightCommand(UserId(httpContext), request.Weight, request.MeasuredAt), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> CorrectWeight(Guid measurementId, WeightRequest request, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new CorrectWeightCommand(measurementId, UserId(httpContext), request.Weight, request.MeasuredAt), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteWeight(Guid measurementId, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new DeleteWeightCommand(measurementId, UserId(httpContext)), ct);
        return TypedResults.NoContent();
    }

    // ---------------------------------------------------------------- Perimeters

    private static async Task<IResult> GetPerimetersHistory(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var history = await mediator.QueryAsync<GetPerimetersHistoryQuery, List<PerimetersResponse>>(
            new GetPerimetersHistoryQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(history);
    }

    private static async Task<IResult> RegisterPerimeters(PerimetersRequest request, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var id = await mediator.SendAsync<RegisterPerimetersCommand, Guid>(
            new RegisterPerimetersCommand(UserId(httpContext), request.ToValues(), request.MeasuredAt), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> CorrectPerimeters(Guid measurementId, PerimetersRequest request, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new CorrectPerimetersCommand(measurementId, UserId(httpContext), request.ToValues(), request.MeasuredAt), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeletePerimeters(Guid measurementId, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new DeletePerimetersCommand(measurementId, UserId(httpContext)), ct);
        return TypedResults.NoContent();
    }

    // ---------------------------------------------------------------- SkinFolds

    private static async Task<IResult> GetSkinFoldsHistory(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var history = await mediator.QueryAsync<GetSkinFoldsHistoryQuery, List<SkinFoldsResponse>>(
            new GetSkinFoldsHistoryQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(history);
    }

    private static async Task<IResult> RegisterSkinFolds(SkinFoldsRequest request, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var id = await mediator.SendAsync<RegisterSkinFoldsCommand, Guid>(
            new RegisterSkinFoldsCommand(UserId(httpContext), request.ToValues(), request.MeasuredAt), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> CorrectSkinFolds(Guid measurementId, SkinFoldsRequest request, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new CorrectSkinFoldsCommand(measurementId, UserId(httpContext), request.ToValues(), request.MeasuredAt), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteSkinFolds(Guid measurementId, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new DeleteSkinFoldsCommand(measurementId, UserId(httpContext)), ct);
        return TypedResults.NoContent();
    }
}
