using LightWeight.shared.BuildingBlocks;
using LightWeight.UserProfile.Domain.Enum;

namespace LightWeight.UserProfile.Domain.Events;

/// <summary>Raised on every modification, even if nothing changed: consumers upsert, so repeating it is harmless</summary>
public sealed record UserProfileModifiedDomainEvent(Guid UserId, string Name, Sex Sex, DateTime OccurredAtUtc) : IDomainEvent;
