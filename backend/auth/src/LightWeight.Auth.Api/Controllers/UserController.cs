using LightWeight.Auth.Api.DTOs;
using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;
using LightWeight.Auth.Application.Commands.LoginWithRefreshToken;
using LightWeight.Auth.Application.Commands.Logout;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.shared.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace LightWeight.Auth.Api.Controllers;
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }
   [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp(
        SendOtpCodeRequest request,
        CancellationToken ct)
    {
         await _mediator.SendAsync<SendOtpCodeCommand>(
            new SendOtpCodeCommand(request.Email), ct);

        return Ok();
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(
        VerifyOtpCodeRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.SendAsync<VerifyOtpCodeCommand, OtpLoginResult>(
            new VerifyOtpCodeCommand(
                request.Code,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                request.DeviceIdentifier,
                request.DeviceName,
                request.Platform,
                request.Email),
            ct);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.SendAsync<LoginWithRefreshTokenCommand, LoginRefreshTokenResult>(
            new LoginWithRefreshTokenCommand(request.RefreshToken, request.Ip), ct);

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken ct)
    {
        await _mediator.SendAsync(
            new LogoutCommand(request.UserId, request.RefreshToken), ct);

        return NoContent();
    }
}