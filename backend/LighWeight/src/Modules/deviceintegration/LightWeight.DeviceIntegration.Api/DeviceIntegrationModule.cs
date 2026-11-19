namespace LightWeight.DeviceIntegration.Api;

using System.Security.Claims;
using LightWeight.DeviceIntegration.Application.Commands.FatSecret;
using LightWeight.DeviceIntegration.Application.Queries.FatSecret;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <param name="OAuthToken">oauth_token of the query string FatSecret sends the user back with</param>
/// <param name="OAuthVerifier">oauth_verifier of the same query string</param>
public sealed record CompleteFatSecretLinkingRequest(string OAuthToken, string OAuthVerifier);

public static class DeviceIntegrationModule
{
    public static IEndpointRouteBuilder MapDeviceIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        // Every endpoint works on the connections of the authenticated user
        var group = app.MapGroup("/api/deviceintegration/fatsecret").RequireAuthorization();

        group.MapGet("/", GetConnection);
        group.MapPost("/link", StartLinking);
        group.MapPost("/link/complete", CompleteLinking);
        group.MapDelete("/", Unlink);

        return app;
    }

    private static Guid UserId(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? throw new UnauthorizedAccessException();
        return Guid.Parse(userId);
    }

    private static async Task<IResult> GetConnection(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        var connection = await mediator.QueryAsync<GetFatSecretConnectionQuery, FatSecretConnectionResponse>(
            new GetFatSecretConnectionQuery(UserId(httpContext)), ct);
        return TypedResults.Ok(connection);
    }

    /// <summary>Returns the FatSecret page the frontend sends the user to</summary>
    private static async Task<IResult> StartLinking(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        string authorizeUrl = await mediator.SendAsync<StartFatSecretLinkingCommand, string>(
            new StartFatSecretLinkingCommand(UserId(httpContext)), ct);
        return TypedResults.Ok(new { authorizeUrl });
    }

    private static async Task<IResult> CompleteLinking(CompleteFatSecretLinkingRequest request, IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new CompleteFatSecretLinkingCommand(UserId(httpContext), request.OAuthToken, request.OAuthVerifier), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> Unlink(IMediator mediator, HttpContext httpContext, CancellationToken ct)
    {
        await mediator.SendAsync(new UnlinkFatSecretCommand(UserId(httpContext)), ct);
        return TypedResults.NoContent();
    }
}
