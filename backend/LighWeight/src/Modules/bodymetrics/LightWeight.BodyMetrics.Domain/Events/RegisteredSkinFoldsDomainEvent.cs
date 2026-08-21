using LightWeight.shared.BuildingBlocks;

namespace LightWeight.bodymetrics.Domain.Events;

public sealed record RegisteredSkinFoldsDomainEvent(Guid SkinFoldId,Guid UserId,DateTime OccurredAtUtc) : IDomainEvent;