namespace LightWeight.Training.Api.DTOs;

public sealed record CreateTrainingTemplateRequest
(
    Guid ProgramId,
    string VolumeLandmark,
    string TrainingDistribution,
    int DurationInDays,
    int Order
);
