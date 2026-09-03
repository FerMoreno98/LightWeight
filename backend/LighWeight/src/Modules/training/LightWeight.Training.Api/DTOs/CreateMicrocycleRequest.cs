namespace LightWeight.Training.Api.DTOs;

public sealed record CreateMicrocycleRequest
(
    Guid MesocycleId,
    int DurationInDays,
    string TrainingDistribution
);
