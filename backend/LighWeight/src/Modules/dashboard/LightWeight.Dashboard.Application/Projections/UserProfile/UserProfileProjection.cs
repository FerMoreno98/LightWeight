namespace LightWeight.Dashboard.Application.Projections.UserProfile;

/// <summary>What the dashboard needs of the profile of a user: the sex, for the fat percentage of the skinfolds</summary>
public sealed class UserProfileProjection
{
    public Guid UserId { get; set; }

    /// <summary>Male or Female, as UserProfile publishes it</summary>
    public string Sex { get; set; } = string.Empty;
}
