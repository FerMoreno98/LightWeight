namespace LightWeight.Training.Api.DTOs;

public sealed record CreateTrainingSessionRequest
(
    Guid TemplateSessionId,
    int MotivationLevel,
    int SleepLevel,
    int DOMSLevel,
    string? Comments
);
