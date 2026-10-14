using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.Events;

/// <summary>A body weight (kg) has been registered</summary>
public sealed record WeightRegisteredDomainEvent(Guid MeasurementId, Guid UserId, decimal Weight, DateTime MeasuredAt) : DomainEvent;

/// <summary>A registered body weight has been corrected; it carries the new values</summary>
public sealed record WeightCorrectedDomainEvent(Guid MeasurementId, Guid UserId, decimal Weight, DateTime MeasuredAt) : DomainEvent;

/// <summary>A registered body weight has been deleted</summary>
public sealed record WeightDeletedDomainEvent(Guid MeasurementId, Guid UserId, DateTime MeasuredAt) : DomainEvent;
