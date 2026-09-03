namespace LightWeight.Training.Api.DTOs;

public sealed record CreateTrainingTemplateRequest
(
    string Name,
    string VolumeLandmark,
    string TrainingDistribution
);
