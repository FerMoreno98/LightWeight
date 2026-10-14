namespace LightWeight.Training.Api;
using System.Security.Claims;
using LightWeight.Training.Api.DTOs;
using LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;
using LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;
using LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;
using LightWeight.Training.Application.Queries.Macrocycles.GetUserMacrocycles;
using LightWeight.Training.Application.Queries.Macrocycles.GetMacrocycleDetail;
using LightWeight.Training.Application.Queries.Mesocycles.GetMesocycleDetail;
using LightWeight.Training.Application.Queries.Microcycles.GetCurrentMicrocycle;
using LightWeight.Training.Application.Queries.SessionTemplates.GetTemplateSessionsFromTrainingTemplate;
using LightWeight.Training.Application.Commands.Microcycles.FinishMicrocycle;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;
using LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;
using LightWeight.Training.Application.Commands.Programs.CreateProgram;
using LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;
using LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;
using LightWeight.Training.Application.Commands.TemplateSuperSets.CreateTemplateSuperSet;
using LightWeight.Training.Application.Commands.TemplateSuperSets.RemoveTemplateSuperSet;
using LightWeight.Training.Application.Commands.TrainingSessions.AssignSuperSet;
using LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.DiscardTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.FinishTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.RegisterSet;
using LightWeight.Training.Application.Commands.TrainingSessions.RemoveSet;
using LightWeight.Training.Application.Commands.TrainingSessions.UpdateSet;
using LightWeight.Training.Application.Queries.TrainingSessions.GetPreviousPerformance;
using LightWeight.Training.Application.Queries.TrainingSessions.GetSessionWorkout;
using Microsoft.AspNetCore.Mvc;
using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Application.Queries.Exercises.GetAllExercises;
using LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;
using LightWeight.Training.Application.Queries.SessionTemplates.GetNumberOfSeriesPerGroupPerSession;
using LightWeight.Training.Application.Queries.TrainingTemplates.GetUserTrainingTemplates;
using LightWeight.Training.Application.Queries.Programs.GetUserPrograms;
using LightWeight.Training.Application.Commands.TemplateSets.DeleteTemplateSet;
using LightWeight.Training.Application.Commands.TemplateSessions.DeleteTemplateSession;
using LightWeight.Training.Application.Commands.TrainingTemplates.DeleteTrainingTemplate;
using LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;
using LightWeight.Training.Application.Commands.TrainingTemplates.RenameTrainingTemplate;
using LightWeight.Training.Application.Commands.TemplateSets.UpdateTemplateSet;

public static class TrainingModule
{
    public static IEndpointRouteBuilder MapTrainingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/training");

        group.MapPost("/macrocycle", CreateMacrocycle).RequireAuthorization();
        group.MapGet("/macrocycles", GetUserMacrocycles).RequireAuthorization();
        group.MapGet("/macrocycle/{macrocycleId:guid}", GetMacrocycleDetail).RequireAuthorization();
        group.MapPost("/macrocycle/{macrocycleId:guid}/finish", FinishMacrocycle).RequireAuthorization();
        group.MapPost("/mesocycle", CreateMesocycle).RequireAuthorization();
        group.MapGet("/mesocycle/{mesocycleId:guid}", GetMesocycleDetail).RequireAuthorization();
        group.MapPost("/mesocycle/{mesocycleId:guid}/finish", FinishMesocycle).RequireAuthorization();
        group.MapPost("/microcycle", CreateMicrocycle).RequireAuthorization();
        group.MapGet("/microcycle/current", GetCurrentMicrocycle).RequireAuthorization();
        group.MapPost("/microcycle/{microcycleId:guid}/finish", FinishMicrocycle).RequireAuthorization();
        group.MapPost("/program", CreateProgram).RequireAuthorization();
        group.MapGet("/programs", GetUserPrograms).RequireAuthorization();
        group.MapPost("/training-template", CreateTrainingTemplate).RequireAuthorization();
        group.MapPost("/template-session", CreateTemplateSession).RequireAuthorization();
        group.MapPost("/template-set", CreateTemplateSet).RequireAuthorization();
        group.MapPost("/template-session/{templateSessionId:guid}/supersets", CreateTemplateSuperSet).RequireAuthorization();
        group.MapDelete("/template-session/{templateSessionId:guid}/supersets/{superSetGroupId:guid}", RemoveTemplateSuperSet).RequireAuthorization();
        group.MapPost("/training-session", CreateTrainingSession).RequireAuthorization();
        group.MapGet("/training-session/workout/{templateSessionId:guid}", GetSessionWorkout).RequireAuthorization();
        group.MapGet("/training-session/previous", GetPreviousPerformance).RequireAuthorization();
        group.MapPost("/training-session/{trainingSessionId:guid}/sets", RegisterSet).RequireAuthorization();
        group.MapPut("/training-session/{trainingSessionId:guid}/sets/{setId:guid}", UpdateSet).RequireAuthorization();
        group.MapDelete("/training-session/{trainingSessionId:guid}/sets/{setId:guid}", RemoveSet).RequireAuthorization();
        group.MapPost("/training-session/{trainingSessionId:guid}/finish", FinishTrainingSession).RequireAuthorization();
        group.MapPut("/training-session/{trainingSessionId:guid}/supersets", AssignSuperSet).RequireAuthorization();
        // DELETE /training-session/{id} already deletes a session of a template, so discarding is a POST
        group.MapPost("/training-session/{trainingSessionId:guid}/discard", DiscardTrainingSession).RequireAuthorization();
        group.MapGet("/exercises", GetAllExercises).RequireAuthorization();
        group.MapGet("/training-template/{trainingTemplateId:guid}/sessions", GetSessionsOfATrainingTemplate).RequireAuthorization();
        group.MapGet("/training-template/{trainingTemplateId:guid}/{sessionTemplateId:guid}/sets",GetSetsOfASessionTemplate).RequireAuthorization();
        group.MapGet("/training-session/{trainingTemplateId:guid}/seriespermusclegrouppersession",GetNumberOfSeriesPerMuscleGroupPerSession).RequireAuthorization();
        group.MapGet("/training-template/trainingTemplates", GetUserTrainingTemplates).RequireAuthorization();
        group.MapDelete("/training-set/{templateSetId:guid}", DeleteTemplateSet).RequireAuthorization();
        group.MapDelete("/training-session/{templateSessionId:guid}",DeleteTemplateSession).RequireAuthorization();
        group.MapDelete("/training-template/{trainingTemplateId:guid}",DeleteTrainingTemplate).RequireAuthorization();
        group.MapPost("/training-template/{trainingTemplateId:guid}/duplicate",DuplicateTrainingTemplate).RequireAuthorization();
        group.MapPatch("/training-template/{trainingTemplateId:guid}/name",RenameTrainingTemplate).RequireAuthorization();
        group.MapPut("/template-set",UpdateTemplateSet).RequireAuthorization();

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

        var id = await mediator.SendAsync<CreateMacrocycleCommand, Guid>(new CreateMacrocycleCommand(
            Guid.Parse(userId),
            request.TrainingStage,
            request.Comments
        ), ct);

        return TypedResults.Ok(new { id });
    }
    private static async Task<IResult> GetUserMacrocycles
    (
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var macrocycles = await mediator.QueryAsync<GetUserMacrocyclesQuery, List<GetUserMacrocyclesResponse>>(
            new GetUserMacrocyclesQuery(Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(macrocycles);
    }
    private static async Task<IResult> GetMacrocycleDetail
    (
        IMediator mediator,
        Guid macrocycleId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var macrocycle = await mediator.QueryAsync<GetMacrocycleDetailQuery, GetMacrocycleDetailResponse>(
            new GetMacrocycleDetailQuery(macrocycleId, Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(macrocycle);
    }
    private static async Task<IResult> FinishMacrocycle
    (
        IMediator mediator,
        Guid macrocycleId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new FinishMacrocycleCommand(macrocycleId, Guid.Parse(userId)), ct);
        return TypedResults.NoContent();
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
        var id = await mediator.SendAsync<CreateMesocycleCommand, Guid>(new CreateMesocycleCommand(
            request.MacrocycleId,
            Guid.Parse(userId),
            request.ProgramId,
            request.MotivationLevel,
            request.Injuries,
            request.Comments
        ), ct);
        return TypedResults.Ok(new { id });
    }
    private static async Task<IResult> GetMesocycleDetail
    (
        IMediator mediator,
        Guid mesocycleId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var mesocycle = await mediator.QueryAsync<GetMesocycleDetailQuery, GetMesocycleDetailResponse>(
            new GetMesocycleDetailQuery(mesocycleId, Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(mesocycle);
    }
    private static async Task<IResult> FinishMesocycle
    (
        IMediator mediator,
        Guid mesocycleId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new FinishMesocycleCommand(mesocycleId, Guid.Parse(userId)), ct);
        return TypedResults.NoContent();
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
        var id = await mediator.SendAsync<CreateMicrocycleCommand, Guid>(new CreateMicrocycleCommand(
            request.MesocycleId,
            Guid.Parse(userId),
            request.TrainingTemplateId
        ), ct);
        return TypedResults.Ok(new { id });
    }
    private static async Task<IResult> GetCurrentMicrocycle
    (
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var current = await mediator.QueryAsync<GetCurrentMicrocycleQuery, GetCurrentMicrocycleResponse>(
            new GetCurrentMicrocycleQuery(Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(current);
    }

    private static async Task<IResult> FinishMicrocycle
    (
        IMediator mediator,
        Guid microcycleId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new FinishMicrocycleCommand(microcycleId, Guid.Parse(userId)), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateProgram
    (
        CreateProgramRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var id = await mediator.SendAsync<CreateProgramCommand, Guid>(new CreateProgramCommand(
            Guid.Parse(userId),
            request.Name,
            request.Periodization,
            request.AimMuscleGroups
        ), ct);
        return TypedResults.Ok(new { id });
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
            request.ProgramId,
            Guid.Parse(userId),
            request.Name,
            request.VolumeLandmark,
            request.TrainingDistribution,
            request.DurationInDays
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
            request.ExpectedRPE,
            request.Series,
            request.AimMuscleGroups
        ), ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> CreateTrainingSession
    (
        CreateTrainingSessionRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var id = await mediator.SendAsync<CreateTrainingSessionCommand, Guid>(new CreateTrainingSessionCommand(
            Guid.Parse(userId),
            request.TemplateSessionId,
            request.MotivationLevel,
            request.SleepLevel,
            request.DOMSLevel,
            request.Comments
        ), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> GetSessionWorkout
    (
        Guid templateSessionId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var workout = await mediator.QueryAsync<GetSessionWorkoutQuery, GetSessionWorkoutResponse>(
            new GetSessionWorkoutQuery(templateSessionId, Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(workout);
    }

    private static async Task<IResult> GetPreviousPerformance
    (
        [FromQuery] Guid[] exerciseIds,
        [FromQuery] Guid? excludeSessionId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var previous = await mediator.QueryAsync<GetPreviousPerformanceQuery, List<GetPreviousPerformanceResponse>>(
            new GetPreviousPerformanceQuery(exerciseIds, excludeSessionId, Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(previous);
    }

    private static async Task<IResult> RegisterSet
    (
        Guid trainingSessionId,
        RegisterSetRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var id = await mediator.SendAsync<RegisterSetCommand, Guid>(new RegisterSetCommand(
            trainingSessionId,
            Guid.Parse(userId),
            request.ExerciseId,
            request.TemplateSetId,
            request.Repetitions,
            request.Weight,
            request.RPE,
            request.IsBodyWeight,
            request.SuperSetGroupId
        ), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> UpdateSet
    (
        Guid trainingSessionId,
        Guid setId,
        UpdateSetRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new UpdateSetCommand(
            trainingSessionId,
            setId,
            Guid.Parse(userId),
            request.ExerciseId,
            request.Repetitions,
            request.Weight,
            request.RPE,
            request.IsBodyWeight
        ), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> RemoveSet
    (
        Guid trainingSessionId,
        Guid setId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new RemoveSetCommand(trainingSessionId, setId, Guid.Parse(userId)), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> FinishTrainingSession
    (
        Guid trainingSessionId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var result = await mediator.SendAsync<FinishTrainingSessionCommand, FinishTrainingSessionResponse>(
            new FinishTrainingSessionCommand(trainingSessionId, Guid.Parse(userId)), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> AssignSuperSet
    (
        Guid trainingSessionId,
        AssignSuperSetRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new AssignSuperSetCommand(trainingSessionId, Guid.Parse(userId), request.SetIds, request.SuperSetGroupId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateTemplateSuperSet
    (
        Guid templateSessionId,
        CreateTemplateSuperSetRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var id = await mediator.SendAsync<CreateTemplateSuperSetCommand, Guid>(
            new CreateTemplateSuperSetCommand(templateSessionId, Guid.Parse(userId), request.TemplateSetIds), ct);
        return TypedResults.Ok(new { id });
    }

    private static async Task<IResult> RemoveTemplateSuperSet
    (
        Guid templateSessionId,
        Guid superSetGroupId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new RemoveTemplateSuperSetCommand(templateSessionId, Guid.Parse(userId), superSetGroupId), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DiscardTrainingSession
    (
        Guid trainingSessionId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new DiscardTrainingSessionCommand(trainingSessionId, Guid.Parse(userId)), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetAllExercises(
        IMediator mediator,
        CancellationToken ct)
    {
        var exercises = await mediator.QueryAsync<GetAllExercisesQuery, List<GetAllExercisesResponse>>(
            new GetAllExercisesQuery(), ct);
        return TypedResults.Ok(exercises);
    }
    private static async Task<IResult> GetSessionsOfATrainingTemplate
    (
        Guid trainingTemplateId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var sessions = await mediator.QueryAsync<GetTemplateSessionsFromTrainingTemplateQuery, List<GetTemplateSessionsFromTrainingTemplateResponse>>
        (
            new GetTemplateSessionsFromTrainingTemplateQuery(trainingTemplateId, Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(sessions);
    }
    private static async Task<IResult> GetSetsOfASessionTemplate
    (
        Guid sessionTemplateId,
        Guid trainingTemplateId,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var sets = await mediator.QueryAsync<GetSetsFromSessionTemplateQuery,List<GetSetsFromSessionTemplateResponse>>
        (
           new GetSetsFromSessionTemplateQuery(sessionTemplateId,trainingTemplateId,Guid.Parse(userId)),ct
        );
        return TypedResults.Ok(sets);
    }
    private static async Task<IResult> GetNumberOfSeriesPerMuscleGroupPerSession
    (
        Guid trainingTemplateId,
        IMediator mediator,
        CancellationToken ct
    )
    {
        var ret = await mediator.QueryAsync<GetNumberOfSeriesPerGroupPerSessionQuery,List<GetNumberOfSeriesPerGroupPerSessionResponse>>(
            new GetNumberOfSeriesPerGroupPerSessionQuery(trainingTemplateId),ct
        );
        return TypedResults.Ok(ret);
    }
    private static async Task<IResult> GetUserTrainingTemplates
    (
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var templates = await mediator.QueryAsync<GetUserTrainingTemplatesQuery,List<GetUserTrainingTemplatesResponse>>(
            new GetUserTrainingTemplatesQuery(Guid.Parse(userId)),ct
        );
        return TypedResults.Ok(templates);
    }
    private static async Task<IResult> GetUserPrograms
    (
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        var programs = await mediator.QueryAsync<GetUserProgramsQuery,List<GetUserProgramsResponse>>(
            new GetUserProgramsQuery(Guid.Parse(userId)),ct
        );
        return TypedResults.Ok(programs);
    }
    private static async Task<IResult> DeleteTemplateSet(
        IMediator mediator,
        Guid templateSetId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
         var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new DeleteTemplateSetCommand(templateSetId,Guid.Parse(userId)),ct);
        return TypedResults.Ok();
    }
    private static async Task<IResult> DeleteTemplateSession
    (
        IMediator mediator,
        Guid TemplateSessionId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new DeleteTemplateSessionCommand(TemplateSessionId,Guid.Parse(userId)),ct);
        return TypedResults.Ok();  
    }
    private static async Task<IResult> DeleteTrainingTemplate(
        IMediator mediator,
        Guid TrainingTemplateId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
         var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new DeleteTrainingTemplateCommand(TrainingTemplateId,Guid.Parse(userId)),ct);
        return TypedResults.Ok();
    }
    private static async Task<IResult> DuplicateTrainingTemplate(
        IMediator mediator,
        Guid trainingTemplateId,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
        var id = await mediator.SendAsync<DuplicateTrainingTemplateCommand, Guid>(
            new DuplicateTrainingTemplateCommand(trainingTemplateId, Guid.Parse(userId)), ct
        );
        return TypedResults.Ok(new { id });
    }
    private static async Task<IResult> RenameTrainingTemplate(
        IMediator mediator,
        Guid trainingTemplateId,
        RenameTrainingTemplateRequest request,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(
            new RenameTrainingTemplateCommand(trainingTemplateId, Guid.Parse(userId), request.Name), ct
        );
        return TypedResults.NoContent();
    }
    private static async Task<IResult> UpdateTemplateSet
    (
        IMediator mediator,
        HttpContext httpContext,
        UpdateTemplateSetRequest request,
        CancellationToken ct
    )
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();
        await mediator.SendAsync(new UpdateTemplateSetCommand
        (
            request.TemplateSessionId,
            Guid.Parse(userId),
            request.SetId,
            request.Min,
            request.Max,
            request.IsDropSet,
            request.IsMyoRep,
            request.IsCluster,
            request.ExpectedRPE,
            request.AimMuscleGroups
        ),ct);
        return TypedResults.Ok();
    }
}
