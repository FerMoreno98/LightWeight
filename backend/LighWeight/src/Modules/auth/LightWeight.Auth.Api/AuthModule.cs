
using System.Security.Claims;
using System.Threading.RateLimiting;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LightWeight.Auth.Domain.Entities;

public static class AuthModule
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/otp/send",    SendOtpCode).RequireRateLimiting(OtpSendPolicy);
        group.MapPost("/otp/verify",  VerifyOtpCode).RequireRateLimiting(OtpVerifyPolicy);
        group.MapPost("/refresh",     RefreshTokenLogin);
        group.MapPost("/logout",      Logout).RequireAuthorization();

        return app;
    }
    private const string OtpSendPolicy = "auth-otp-send";
    private const string OtpVerifyPolicy = "auth-otp-verify";

    /// <summary>
    /// Limits per IP on the OTP endpoints: sending stops the API from being used to flood inboxes,
    /// verifying stops guessing codes (on top of OtpCode.MaxFailedAttempts). Over the limit: 429.
    /// The IP is the real one only with UseForwardedHeaders behind the proxy; otherwise every user
    /// would share the proxy's IP and its limit
    /// </summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(OtpSendPolicy, httpContext => PerIp(httpContext, permitLimit: 5));
            options.AddPolicy(OtpVerifyPolicy, httpContext => PerIp(httpContext, permitLimit: 10));
        });
        return services;
    }

    private static RateLimitPartition<string> PerIp(HttpContext httpContext, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
            });

    private const string RefreshCookieName = "refresh_token";

    private static CookieOptions RefreshCookieOptions(HttpContext httpContext) => new()
    {
        HttpOnly = true,
        // Only over HTTPS. Auth:RefreshCookie:Secure is false only in Development (plain http on localhost);
        // without the setting it is true, so production can never send it unencrypted by mistake
        Secure = httpContext.RequestServices.GetRequiredService<IConfiguration>()
            .GetValue("Auth:RefreshCookie:Secure", defaultValue: true),
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",         // la cookie solo se envía a rutas de auth, no a toda la app
    };

    private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        CookieOptions options = RefreshCookieOptions(httpContext);
        options.Expires = DateTimeOffset.UtcNow.AddDays(RefreshToken.LifetimeDays);
        httpContext.Response.Cookies.Append(RefreshCookieName, refreshToken, options);
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
        if (!httpContext.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
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
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var user = httpContext.User; 
        string? id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException();

        // Without the cookie there is nothing to revoke: the front clears its token anyway
        if (httpContext.Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            await mediator.SendAsync(new LogoutCommand(Guid.Parse(id), refreshToken), ct);
        }

        httpContext.Response.Cookies.Delete(RefreshCookieName, RefreshCookieOptions(httpContext));
        return TypedResults.NoContent();
    }
}