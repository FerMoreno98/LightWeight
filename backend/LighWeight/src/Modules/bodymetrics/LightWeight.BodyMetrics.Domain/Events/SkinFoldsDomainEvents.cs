using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.Events;

/// <summary>A measurement of skinfolds (mm) has been registered</summary>
public sealed record SkinFoldsRegisteredDomainEvent(Guid MeasurementId, Guid UserId, SkinFoldMeasures Measures, DateTime MeasuredAt) : DomainEvent;

/// <summary>A measurement of skinfolds has been corrected; it carries the new values</summary>
public sealed record SkinFoldsCorrectedDomainEvent(Guid MeasurementId, Guid UserId, SkinFoldMeasures Measures, DateTime MeasuredAt) : DomainEvent;

/// <summary>A measurement of skinfolds has been deleted</summary>
public sealed record SkinFoldsDeletedDomainEvent(Guid MeasurementId, Guid UserId, DateTime MeasuredAt) : DomainEvent;
