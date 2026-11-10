using LightWeight.shared.Exceptions;

namespace LightWeight.UserProfile.Application.Exceptions;

/// <summary>The onboarding was already done: the profile is changed with UpdateProfile</summary>
public sealed class UserProfileAlreadyCompletedException : UserProfileApplicationException, IConflictException
{
    public UserProfileAlreadyCompletedException() : base("The profile is already completed")
    {
    }
}
