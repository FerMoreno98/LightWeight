using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.SessionTemplates.GetSessionFromTrainingTemplate;

public sealed class GetSessionsFromTrainingTemplateQueryHandler : IQueryHandler<GetSessionsFromTrainingTemplateQuery, List<GetSessionsFromTrainingTemplateResponse>>
{
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;

    public GetSessionsFromTrainingTemplateQueryHandler(ITrainingTemplateRepository trainingTemplateRepository)
    {
        _trainingTemplateRepository = trainingTemplateRepository;
    }

    public async Task<List<GetSessionsFromTrainingTemplateResponse>> HandleAsync(GetSessionsFromTrainingTemplateQuery query, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _trainingTemplateRepository.GetByIdAsync(query.TrainingTemplateId)
        ?? throw new Exception();
        List<GetSessionsFromTrainingTemplateResponse> ret = new List<GetSessionsFromTrainingTemplateResponse>();
        foreach(var session in trainingTemplate.TemplateSessions)
        {
            var sessionRet = new GetSessionsFromTrainingTemplateResponse(session.Id,session.Name);
            ret.Add(sessionRet);
        }
        return ret;
    }
}