namespace LightWeight.Training.Api;
using System.Security.Claims;
using LightWeight.Training.Api.DTOs;
using LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;
using LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;
using LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;
using LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;
using LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;
using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Application.Queries.Exercises.GetAllExercises;
using LightWeight.Training.Application.Queries.SessionTemplates.GetSessionFromTrainingTemplate;
using LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;

public static class TrainingModule
{
    public static IEndpointRouteBuilder MapTrainingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training");

        group.MapPost("/macrocycle", CreateMacrocycle).RequireAuthorization();
        group.MapPost("/mesocycle", CreateMesocycle).RequireAuthorization();
        group.MapPost("/microcycle", CreateMicrocycle).RequireAuthorization();
        group.MapPost("/training-template", CreateTrainingTemplate).RequireAuthorization();
        group.MapPost("/template-session", CreateTemplateSession).RequireAuthorization();
        group.MapPost("/template-set", CreateTemplateSet).RequireAuthorization();
        group.MapPost("/training-session", CreateTrainingSession).RequireAuthorization();
        group.MapGet("/exercises", GetAllExercises).RequireAuthorization();
        group.MapGet("/training-template/{templateId:guid}/sessions", GetSessionsOfATrainingTemplate).RequireAuthorization();
        group.MapGet("/training-template/{trainingTemplateId:guid}/{sessionTemplateId:guid}/sets",GetSetsOfASessionTemplate).RequireAuthorization();

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
    private static async Task<IResult> CreateMesocycle
    (
        CreateMesocycleRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new CreateMesocycleCommand(
            request.MacrocycleId,
            Guid.Parse(userId),
            request.aimMuscleGroups,
            request.MotivationLevel,
            request.Injuries,
            request.Comments,
            request.StartAt,
            request.EndAt
        ));
        return TypedResults.Ok();
    }

    private static async Task<IResult> CreateMicrocycle
    (
        CreateMicrocycleRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new CreateMicrocycleCommand(
            request.MesocycleId,
            Guid.Parse(userId),
            request.DurationInDays,
            request.TrainingDistribution
        ), ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> CreateTrainingTemplate
    (
        CreateTrainingTemplateRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var id = await mediator.SendAsync<CreateTrainingTemplateCommand, Guid>(new CreateTrainingTemplateCommand(
            Guid.Parse(userId),
            request.Name,
            request.VolumeLandmark,
            request.TrainingDistribution
        ), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> CreateTemplateSession
    (
        CreateTemplateSessionRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var id = await mediator.SendAsync<CreateTemplateSessionCommand, Guid>(new CreateTemplateSessionCommand(
            request.TrainingTemplateId,
            Guid.Parse(userId),
            request.Name
        ), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> CreateTemplateSet
    (
        CreateTemplateSetRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new CreateTemplateSetCommand(
            request.ExerciseId,
            request.TemplateSessionId,
            Guid.Parse(userId),
            request.Min,
            request.Max,
            request.IsDropSet,
            request.IsMyoRep,
            request.IsCluster,
            request.ExpectedRIR,
            request.AimMuscleGroups,
            request.SuperSetGroupId
        ), ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> CreateTrainingSession
    (
        CreateTrainingSessionRequest request,
        IMediator mediator,
        CancellationToken ct
    )
    {
        await mediator.SendAsync(new CreateTrainingSessionCommand(
            request.MicrocycleId,
            request.Name,
            request.Comments,
            request.MotivationLevel,
            request.SleepLevel,
            request.DOMSLevel
        ), ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> GetAllExercises(
        IMediator mediator,
        CancellationToken ct)
    {
        var exercises = await mediator.QueryAsync<GetAllExercisesQuery, List<GetAllExercisesResponse>>(
            new GetAllExercisesQuery(), ct);
        return TypedResults.Ok(exercises);
    }
    private static async Task<IResult> GetSessionsOfATrainingTemplate(
        Guid templateId,
        IMediator mediator,
        CancellationToken ct)
    {
        var sessions = await mediator.QueryAsync<GetSessionsFromTrainingTemplateQuery, List<GetSessionsFromTrainingTemplateResponse>>
        (
            new GetSessionsFromTrainingTemplateQuery(templateId), ct
        );
        return TypedResults.Ok(sessions);
    }
    private static async Task<IResult> GetSetsOfASessionTemplate
    (
        Guid sessionTemplateId,
        Guid trainingTemplateId,
        IMediator mediator,
        CancellationToken ct
    )
    {
        var sets = await mediator.QueryAsync<GetSetsFromSessionTemplateQuery,List<GetSetsFromSessionTemplateResponse>>
        (
           new GetSetsFromSessionTemplateQuery(sessionTemplateId,trainingTemplateId),ct
        );
        return TypedResults.Ok(sets);
    }
    
}
