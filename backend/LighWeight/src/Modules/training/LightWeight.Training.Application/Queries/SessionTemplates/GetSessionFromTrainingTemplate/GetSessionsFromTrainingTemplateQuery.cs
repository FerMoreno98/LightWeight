using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.SessionTemplates.GetSessionFromTrainingTemplate;

public sealed record GetSessionsFromTrainingTemplateQuery(Guid TrainingTemplateId) : IQuery<List<GetSessionsFromTrainingTemplateResponse>>;

public record GetSessionsFromTrainingTemplateResponse
(
    Guid Id,
    string Name
);