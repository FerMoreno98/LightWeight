using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;

/// <summary>
/// Duplicates a training template with its sessions and sets. Returns the id of the copy.
/// Name and VolumeLandmark are optional: without them the copy is "{name} (copia)" with the same landmark
/// </summary>
public sealed record DuplicateTrainingTemplateCommand
(
    Guid TrainingTemplateId,
    Guid UserId,
    string? Name = null,
    string? VolumeLandmark = null
) : ICommand<Guid>;
