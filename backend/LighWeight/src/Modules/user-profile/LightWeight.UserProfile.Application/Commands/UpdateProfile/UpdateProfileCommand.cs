using System.Collections.Specialized;
using LightWeight.shared.Mediator;

namespace LightWeight.UserProfile.Application.Commands.UpdateProfile;

public sealed record UpdateProfileCommand
(
    Guid UserId,
    string Name,
    string Sex,
    DateTime DateOfBirth
) : ICommand;