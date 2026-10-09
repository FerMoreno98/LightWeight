namespace LightWeight.Training.Api.DTOs;

public sealed record CreateMacrocycleRequest
(
    string TrainingStage,
    string? Comments
);
