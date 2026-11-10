using LightWeight.shared.BuildingBlocks;
using LightWeight.UserProfile.Domain.Enum;

namespace LightWeight.UserProfile.Domain.Events;

/// <summary>Raised on every modification, even if the sex did not change: consumers upsert, so repeating it is harmless</summary>
public sealed record UserProfileModifiedDomainEvent(Guid UserId, Sex Sex, DateTime OccurredAtUtc) : IDomainEvent;
