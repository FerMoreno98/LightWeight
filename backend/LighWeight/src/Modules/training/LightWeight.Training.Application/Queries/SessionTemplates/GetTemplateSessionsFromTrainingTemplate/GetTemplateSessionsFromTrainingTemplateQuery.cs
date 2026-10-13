using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.SessionTemplates.GetTemplateSessionsFromTrainingTemplate;

/// <summary>Sessions of a template of the user, in the order they were created</summary>
public sealed record GetTemplateSessionsFromTrainingTemplateQuery(Guid TrainingTemplateId, Guid UserId) : IQuery<List<GetTemplateSessionsFromTrainingTemplateResponse>>;

/// <param name="SetsCount">Number of planned sets of the session</param>
/// <param name="ExercisesCount">Number of different exercises of the session</param>
public sealed record GetTemplateSessionsFromTrainingTemplateResponse
(
    Guid Id,
    string Name,
    int SetsCount,
    int ExercisesCount
);
