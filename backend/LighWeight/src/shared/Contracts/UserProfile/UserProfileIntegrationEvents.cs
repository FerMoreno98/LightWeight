namespace LightWeight.shared.Contracts.UserProfile;

/// <summary>
/// The profile of a user was completed or modified, or the dashboard asked to publish it again (rebuild).
/// Same idea as the "Recorded" events of bodymetrics: it always carries the current value, so consumers upsert it.
/// Only the data other modules need: the sex, for the fat percentage of the skinfolds
/// </summary>
/// <param name="Sex">Male or Female</param>
public sealed record ProfileRecordedIntegrationEvent(
    Guid     UserId,
    string   Sex,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.UserProfile.ProfileRecorded;
}
