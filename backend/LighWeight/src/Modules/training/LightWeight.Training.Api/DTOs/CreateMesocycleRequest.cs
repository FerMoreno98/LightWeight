namespace LightWeight.Training.Api.DTOs;

public sealed record CreateMesocycleRequest
(
    Guid MacrocycleId,
    Guid ProgramId,
    int MotivationLevel,
    string? Injuries,
    string? Comments,
    DateTime StartAt,
    DateTime EndAt
);