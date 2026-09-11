using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DeleteTrainingTemplate;

public sealed record DeleteTrainingTemplateCommand(Guid TrainingTemplateId, Guid UserId) : ICommand;