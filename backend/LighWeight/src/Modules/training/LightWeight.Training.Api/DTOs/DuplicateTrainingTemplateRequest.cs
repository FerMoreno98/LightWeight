namespace LightWeight.Training.Api.DTOs;

/// <summary>Both optional: null keeps "{name} (copia)" and the original landmark</summary>
public sealed record DuplicateTrainingTemplateRequest
(
    string? Name,
    string? VolumeLandmark
);
