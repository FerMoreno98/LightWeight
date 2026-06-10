
using LightWeight.Auth.Application.Exceptions;
using LightWeight.Auth.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LightWeight.Auth.Api.Middlewares;
// en el constructor podria ir un log, pero de momento no lo voy a implementar
// Este es un manejo de excepciones global para las excepciones de application y dominio
internal sealed class AuthExceptionHandler() : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync
    (
        HttpContext httpContext,
        Exception exception, 
        CancellationToken cancellationToken
    )
    {

        var (statusCode, title) = exception switch
        {
            // Excepciones de aplicación
            UserNotFoundException          => (404, "User not found"),

            // Excepciones de dominio — OTP
            // OtpCodeExpiredException        => (400, "OTP code expired"),
            // OtpCodeAlreadyUsedException    => (400, "OTP code already used"),
            InvalidOtpCodeException        => (400, "Invalid OTP code"),

            // Excepciones de dominio — tokens
            InvalidRefreshTokenException   => (401, "Invalid refresh token"),
            RevokedRefreshTokenException   => (401, "Refresh token revoked"),
            StolenRefreshTokenException    => (401, "Session invalidated"),

            // Excepciones de dominio — device
            DeviceNotFoundException        => (404, "Device not found"),
            RefreshTokenNotFoundException  => (404, "Refresh token not found"),

            // No es una excepción que este handler conozca
            _ => (-1, null)
        };

        if (statusCode == -1)
            return false; // deja que pase al siguiente handler

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Type   = exception.GetType().Name
        };

        // Caso especial: token robado → incluir UserId para que el front
        // pueda disparar el logout total
        if (exception is StolenRefreshTokenException stolen)
            problem.Extensions["userId"] = stolen.UserId;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}