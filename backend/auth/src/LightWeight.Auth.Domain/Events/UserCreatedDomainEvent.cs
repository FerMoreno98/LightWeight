using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.Events;

public sealed record UserCreatedDomainEvent(string Email,  DateTime OccurredAtUtc) : IDomainEvent;