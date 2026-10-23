using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Events;

/// <summary>A training session has been finished: from now on it is read only</summary>
public sealed record TrainingSessionFinishedDomainEvent(Guid SessionId, Guid UserId, Guid MicrocycleId, DateTime FinishedAt) : DomainEvent;
