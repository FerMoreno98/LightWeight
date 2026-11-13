using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.ChangeTrainingTemplateVolumeLandmark;

/// <summary>Changes the volume landmark (MV, MEV, MAV, MRV) of a training template</summary>
public sealed record ChangeTrainingTemplateVolumeLandmarkCommand(Guid TrainingTemplateId, Guid UserId, string VolumeLandmark) : ICommand;
