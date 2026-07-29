namespace LightWeight.Training.Api.DTOs;

public sealed record CreateMacrocycleRequest
(
    DateTime StartAt,
    DateTime? EndAt,
    string TrainingStage,
    string Periodization,
    string? Comments
);
