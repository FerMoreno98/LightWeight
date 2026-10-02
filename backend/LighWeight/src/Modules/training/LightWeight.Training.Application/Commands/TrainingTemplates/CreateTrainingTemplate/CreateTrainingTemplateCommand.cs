using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed record CreateTrainingTemplateCommand
(
    Guid ProgramId,
    Guid UserId,
    string VolumeLandmark,
    string TrainingDistribution,
    int DurationInDays,
    int Order
) : ICommand<Guid>;

