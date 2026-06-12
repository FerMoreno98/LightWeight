using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.Events;

public sealed record UserCreatedDomainEvent(Guid UserId, string Email, DateTime OccurredAtUtc) : IDomainEvent;