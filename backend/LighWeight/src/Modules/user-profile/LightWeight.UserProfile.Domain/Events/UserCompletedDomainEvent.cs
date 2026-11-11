using LightWeight.shared.BuildingBlocks;
using LightWeight.UserProfile.Domain.Enum;

namespace LightWeight.UserProfile.Domain.Events;

public sealed record UserCompletedDomainEvent(Guid UserId, string Name, Sex Sex, DateTime OccurredAtUtc) : IDomainEvent;
