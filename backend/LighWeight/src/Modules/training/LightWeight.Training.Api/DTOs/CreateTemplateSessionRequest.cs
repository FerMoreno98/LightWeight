namespace LightWeight.Training.Api.DTOs;

public sealed record CreateTemplateSessionRequest
(
    Guid TrainingTemplateId,
    string Name
);
