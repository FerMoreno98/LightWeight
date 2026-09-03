using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed record CreateTrainingTemplateCommand
(
    Guid UserId,
    string Name,
    string VolumeLandmark,
    string TrainingDistribution
) : ICommand<Guid>;
