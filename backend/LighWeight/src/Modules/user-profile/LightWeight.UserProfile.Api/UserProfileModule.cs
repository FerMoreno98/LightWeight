
using LightWeight.UserProfile.Api.DTOs;
using LightWeight.UserProfile.Application.Commands.CompleteProfile;
using LightWeight.UserProfile.Application.Commands.UpdateProfile;
using LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public static class UserProfileModule
{
    public static IEndpointRouteBuilder MapUserProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user-profile").RequireAuthorization();

        group.MapPost("/complete",        CompleteProfile);
        group.MapPut("/",                 UpdateProfile);
        group.MapPatch("/training-stage", ChangeTrainingStage);

        return app;
    }

    private static async Task<IResult> CompleteProfile(
        CompleteProfileRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
         var birthDateUtc = request.BirthDate.Kind == DateTimeKind.Utc 
        ? request.BirthDate 
        : DateTime.SpecifyKind(request.BirthDate, DateTimeKind.Utc);
        Guid UserId = GetUserId(httpContext);   
        await mediator.SendAsync(
            new CompleteProfileCommand(
                UserId,
                request.FullName,
                birthDateUtc,
                request.Sex,
                request.Stage), ct);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var birthDateUtc = request.DateOfBirth.Kind == DateTimeKind.Utc 
        ? request.DateOfBirth 
        : DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc);
        Guid UserId = GetUserId(httpContext); 
        await mediator.SendAsync(
            new UpdateProfileCommand(
                UserId,
                request.FullName,
                request.Sex,
                birthDateUtc), ct);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ChangeTrainingStage(
        ChangeTrainingStageRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        Guid UserId = GetUserId(httpContext); 
        await mediator.SendAsync(
            new ChangeTrainingStageCommand(
                UserId,
                request.NewStage), ct);

        return TypedResults.NoContent();
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var user = httpContext.User; 
        
        string? id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(id))
        {
            throw new UnauthorizedAccessException("No se encontró el identificador del usuario en los claims.");
        }

        if (!Guid.TryParse(id, out var userId))
        {
            throw new UnauthorizedAccessException("El identificador del usuario no es un GUID válido.");
        }

        return userId;
    }
}