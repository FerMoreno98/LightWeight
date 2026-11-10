using LightWeight.shared.Mediator;

namespace LightWeight.UserProfile.Application.Queries.GetProfile;

/// <summary>
/// The profile of the user. Throws <see cref="Exceptions.UserNotFoundException"/> (404) while the
/// onboarding is not done, which is how the front knows it has to show it
/// </summary>
public sealed record GetProfileQuery(Guid UserId) : IQuery<ProfileResponse>;

/// <param name="Sex">Male or Female</param>
public sealed record ProfileResponse
(
    string Name,
    string Sex,
    DateTime DateOfBirth
);
