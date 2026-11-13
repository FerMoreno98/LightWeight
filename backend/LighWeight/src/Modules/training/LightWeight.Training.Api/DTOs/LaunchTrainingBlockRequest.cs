namespace LightWeight.Training.Api.DTOs;

public sealed record LaunchTrainingBlockRequest
(
    string TrainingStage,
    string? MacrocycleComments,
    Guid ProgramId,
    int MotivationLevel,
    string? Injuries,
    string? MesocycleComments,
    Guid TrainingTemplateId
);
