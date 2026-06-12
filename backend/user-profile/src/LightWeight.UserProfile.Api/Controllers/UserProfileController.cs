using System.Security.Claims;
using LightWeight.shared.Mediator;
using LightWeight.UserProfile.Api.DTOs;
using LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;
using LightWeight.UserProfile.Application.Commands.CompleteProfile;
using LightWeight.UserProfile.Application.Commands.UpdateProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LightWeight.UserProfile.Api.Controllers;
[Authorize]
[ApiController]
[Route("userprofile")]
public class UserProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteUserProfile
    (
        [FromBody] CompleteUserProfileRequest request,
        CancellationToken ct
    )
    {
        Guid UserId = GetUserId();
        await _mediator.SendAsync
        (new CompleteProfileCommand
        (
            UserId,
            request.Name,
            request.DateOfBirth,
            request.Sex,
            request.CurrentStage
        ));
        return Created();
    }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateUserProfile
    (
       [FromBody] UpdateUserProfileRequest request,
        CancellationToken ct
    )
    {
        Guid UserId = GetUserId();
        await _mediator.SendAsync(
            new UpdateProfileCommand
            (
                UserId,
                request.Name,
                request.Sex,
                request.DateOfBirth
            )
        );
        return NoContent();
    }
    [HttpPut("updatestage")]
    public async Task<IActionResult> UpdateTrainingStage
    (
        [FromBody] ChangeTrainingStageRequest request,
        CancellationToken ct
    )
    {
        
        Guid UserId = GetUserId();
        await _mediator.SendAsync(
            new ChangeTrainingStagesCommand
            (
                UserId,
                request.Stage
            )
        );
        return NoContent();
    }
    private Guid GetUserId()
    {
        string? id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(id))
        {
            throw new UnauthorizedAccessException("No se encontró el identificador del usuario en los claims.");
        }

        return Guid.Parse(id);
    }
    
}