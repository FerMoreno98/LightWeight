namespace LightWeight.Training.Api.DTOs;

public sealed record CreateMesocycleRequest
(
    Guid MacrocycleId,
    List<string> aimMuscleGroups,
    int MotivationLevel,
    string Injuries,
    string Comments,
    DateTime StartAt,
    DateTime EndAt
);