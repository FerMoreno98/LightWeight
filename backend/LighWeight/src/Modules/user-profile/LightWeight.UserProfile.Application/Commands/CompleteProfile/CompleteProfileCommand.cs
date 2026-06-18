using LightWeight.shared.Mediator;

namespace LightWeight.UserProfile.Application.Commands.CompleteProfile;

public sealed record CompleteProfileCommand
(
    Guid UserId,
    string Name,
    DateTime DateOfBirth,
    string Sex,
    string CurrentStage
) : ICommand;