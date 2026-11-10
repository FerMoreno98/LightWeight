using LightWeight.shared.BuildingBlocks;
using LightWeight.UserProfile.Domain.Enum;

namespace LightWeight.UserProfile.Domain.Events;

public sealed record UserCompletedDomainEvent(Guid UserId, Sex Sex, DateTime OccurredAtUtc) : IDomainEvent;
