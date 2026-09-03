namespace LightWeight.Training.Api.DTOs;

public sealed record CreateTrainingSessionRequest
(
    Guid MicrocycleId,
    string Name,
    string? Comments,
    int MotivationLevel,
    int SleepLevel,
    int DOMSLevel
);
