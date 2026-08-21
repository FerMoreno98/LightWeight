using LightWeight.shared.BuildingBlocks;

namespace LightWeight.bodymetrics.Domain.Events;

public sealed record RegisteredPerimetersDomainEvent(Guid PerimeterId,Guid UserId,DateTime OccurredAtUtc) : IDomainEvent;