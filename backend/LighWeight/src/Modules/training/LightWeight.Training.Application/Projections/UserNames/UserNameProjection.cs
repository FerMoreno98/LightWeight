namespace LightWeight.Training.Application.Projections.UserNames;

/// <summary>
/// What training needs of the profile of a user: the name, to show the author of a shared or copied program.
/// Projected from UserProfile (ProfileRecordedIntegrationEvent), so it always holds the current name
/// </summary>
public sealed class UserNameProjection
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;
}
