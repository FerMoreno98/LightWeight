namespace LightWeight.shared.Contracts.UserProfile;
public sealed record ProfileCompletedIntegrationEvent(
    Guid     UserId,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.UserProfile.ProfileCompleted;
}

public sealed record StageChangedIntegrationEvent(
    Guid     UserId,
    string   NewStage,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.UserProfile.StageChanged;
}