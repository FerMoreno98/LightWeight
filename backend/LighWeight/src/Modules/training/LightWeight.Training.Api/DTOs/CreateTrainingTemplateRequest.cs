namespace LightWeight.Training.Api.DTOs;

public sealed record CreateTrainingTemplateRequest
(
    Guid ProgramId,
    string Name,
    string VolumeLandmark,
    string TrainingDistribution,
    int DurationInDays
);
