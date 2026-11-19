using LightWeight.Nutrition.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Nutrition.Domain.Events;

/// <summary>The diary of a day has been recorded for the first time</summary>
public sealed record DiaryDayRecordedDomainEvent(Guid DiaryDayId, Guid UserId, DateOnly Date, DailyIntake Intake) : DomainEvent;

/// <summary>The totals of a recorded day have changed; it carries the new ones</summary>
public sealed record DiaryDayUpdatedDomainEvent(Guid DiaryDayId, Guid UserId, DateOnly Date, DailyIntake Intake) : DomainEvent;

/// <summary>A recorded day no longer has entries in the diary and has been deleted</summary>
public sealed record DiaryDayDeletedDomainEvent(Guid DiaryDayId, Guid UserId, DateOnly Date) : DomainEvent;
