namespace LightWeight.shared.Contracts.Dashboard;

/// <summary>
/// The dashboard has emptied the projections of a user and asks the source modules to publish again the current
/// state of that user's data (one "Recorded" event per row), so the projections are filled from scratch.
/// Every module that feeds the dashboard subscribes to it; the dashboard does not know which ones answer
/// </summary>
public sealed record RebuildRequestedIntegrationEvent(
    Guid     UserId,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.Dashboard.RebuildRequested;
}
