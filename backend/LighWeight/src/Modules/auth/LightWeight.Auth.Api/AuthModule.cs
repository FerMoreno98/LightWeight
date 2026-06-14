
using LightWeight.Auth.Api.DTOs;
using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;
using LightWeight.Auth.Application.Commands.LoginWithRefreshToken;
using LightWeight.Auth.Application.Commands.Logout;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

public static class AuthModule
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/otp/send",    SendOtpCode);
        group.MapPost("/otp/verify",  VerifyOtpCode);
        group.MapPost("/refresh",     RefreshTokenLogin);
        group.MapPost("/logout",      Logout).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> SendOtpCode(
        SendOtpCodeRequest request,
        IMediator mediator)
    {
        await mediator.SendAsync(new SendOtpCodeCommand(request.Email));
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<OtpLoginResponse>, UnauthorizedHttpResult>> VerifyOtpCode(
        VerifyOtpCodeRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.SendAsync<VerifyOtpCodeCommand,OtpLoginResult>(new VerifyOtpCodeCommand
        (
            request.Code,
            ip,
            request.DeviceIdentifier, 
            request.DeviceName,
            request.Platform, 
            request.Email
        ),ct);

        return result is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new OtpLoginResponse(result.RefreshToken,result.AccessToken));
    }

    private static async Task<Results<Ok<RefreshTokenResponse>, UnauthorizedHttpResult>> RefreshTokenLogin(
        RefreshTokenRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.SendAsync<LoginWithRefreshTokenCommand,LoginRefreshTokenResult>
        (
            new LoginWithRefreshTokenCommand
            (
                request.RefreshToken,
                ip
            ), ct);

        return result is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new RefreshTokenResponse(result.AccessToken, result.RefreshToken));
    }

    private static async Task<IResult> Logout(
        LogoutRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.SendAsync
        (
            new LogoutCommand
            (
                request.UserId,
                request.RefreshToken
            ), ct);
        return TypedResults.NoContent();
    }
}