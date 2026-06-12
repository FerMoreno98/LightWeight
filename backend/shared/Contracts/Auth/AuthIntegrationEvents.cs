namespace LightWeight.shared.Contracts.Auth;

public sealed record UserCreatedIntegrationEvent(
    Guid     UserId,
    string   Email,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.Auth.UserCreated;
}