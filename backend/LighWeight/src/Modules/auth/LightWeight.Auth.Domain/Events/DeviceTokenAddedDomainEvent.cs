using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.Events;

public sealed record DeviceTokenAddedDomainEvent
(
    Guid Id,
    Guid UserId, 
    string DeviceIdentifier,
    string DeviceName, 
    string Platform, 
    DateTime LastSeenAt, 
    DateTime CreatedAt, 
    DateTime? RevokedAt,
    DateTime OccurredAtUtc
) : IDomainEvent;