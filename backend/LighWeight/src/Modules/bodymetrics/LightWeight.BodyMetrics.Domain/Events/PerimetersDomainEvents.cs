using LightWeight.BodyMetrics.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.BodyMetrics.Domain.Events;

/// <summary>A measurement of perimeters (cm) has been registered</summary>
public sealed record PerimetersRegisteredDomainEvent(Guid MeasurementId, Guid UserId, PerimeterMeasures Measures, DateTime MeasuredAt) : DomainEvent;

/// <summary>A measurement of perimeters has been corrected; it carries the new values</summary>
public sealed record PerimetersCorrectedDomainEvent(Guid MeasurementId, Guid UserId, PerimeterMeasures Measures, DateTime MeasuredAt) : DomainEvent;

/// <summary>A measurement of perimeters has been deleted</summary>
public sealed record PerimetersDeletedDomainEvent(Guid MeasurementId, Guid UserId, DateTime MeasuredAt) : DomainEvent;
