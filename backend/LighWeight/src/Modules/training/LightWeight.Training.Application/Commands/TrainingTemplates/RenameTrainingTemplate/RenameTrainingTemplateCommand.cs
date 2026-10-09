using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.RenameTrainingTemplate;

/// <summary>Changes the name of a training template</summary>
public sealed record RenameTrainingTemplateCommand(Guid TrainingTemplateId, Guid UserId, string Name) : ICommand;
