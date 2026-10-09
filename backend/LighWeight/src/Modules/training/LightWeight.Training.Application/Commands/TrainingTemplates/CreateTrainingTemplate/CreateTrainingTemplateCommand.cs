using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed record CreateTrainingTemplateCommand
(
    Guid ProgramId,
    Guid UserId,
    string Name,
    string VolumeLandmark,
    string TrainingDistribution,
    int DurationInDays
) : ICommand<Guid>;

