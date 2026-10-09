using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;

/// <summary>Duplicates a training template with its sessions and sets. Returns the id of the copy</summary>
public sealed record DuplicateTrainingTemplateCommand(Guid TrainingTemplateId, Guid UserId) : ICommand<Guid>;
