
using System.Security.Claims;
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
    private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        // hardcodeando valores solo para probar, cambiarlos a variables de entorno en appsettings
        httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,              // solo HTTPS (en dev con http, pon false o usa https local)
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",         // la cookie solo se envía a rutas de auth, no a toda la app
            Expires = DateTimeOffset.UtcNow.AddDays(30) // según tu política de rotation
        });
    }

    private static async Task<IResult> SendOtpCode(
        SendOtpCodeRequest request,
        IMediator mediator)
    {
        await mediator.SendAsync(new SendOtpCodeCommand(request.Email));
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<string>, UnauthorizedHttpResult>> VerifyOtpCode(
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
        SetRefreshTokenCookie(httpContext, result.RefreshToken);

        return TypedResults.Ok(result.AccessToken);
    }

    private static async Task<Results<Ok<string>, UnauthorizedHttpResult>> RefreshTokenLogin(
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!httpContext.Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
        {
            return TypedResults.Unauthorized();
        }
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.SendAsync<LoginWithRefreshTokenCommand,LoginRefreshTokenResult>
        (
            new LoginWithRefreshTokenCommand
            (
                refreshToken,
                ip
            ), 
            ct
        );

        SetRefreshTokenCookie(httpContext, result.RefreshToken);

        return TypedResults.Ok(result.AccessToken);
    }

    private static async Task<IResult> Logout(
        LogoutRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var user = httpContext.User; 
        string? id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException();

        httpContext.Request.Cookies.TryGetValue("refresh_token", out var refreshToken);

        await mediator.SendAsync(new LogoutCommand(Guid.Parse(id), refreshToken), ct);

        httpContext.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
        return TypedResults.NoContent();
    }
}