using LightWeight.shared.BuildingBlocks;

namespace LightWeight.UserProfile.Domain.Events;

public sealed record UserCompletedDomainEvent(Guid UserId,DateTime OccurredAtUtc) : IDomainEvent;