using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.TrainingTemplates.GetUserTrainingTemplates;

public sealed record GetUserTrainingTemplatesQuery(Guid UserId) : IQuery<List<GetUserTrainingTemplatesResponse>>;

public sealed record GetUserTrainingTemplatesResponse
(
    Guid Id,
    Guid ProgramId,
    string ProgramName,
    int Order,
    int DurationInDays,
    string VolumeLandmark,
    string TrainingDistribution,
    Dictionary<string,int> TotalVolume
);

