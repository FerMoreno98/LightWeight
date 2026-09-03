using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.SessionTemplates.GetNumberOfSeriesPerGroupPerSession;

public sealed class GetNumberOfSeriesPerGroupPerSessionQueryHandler : IQueryHandler<GetNumberOfSeriesPerGroupPerSessionQuery, List<GetNumberOfSeriesPerGroupPerSessionResponse>>
{
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;

    public GetNumberOfSeriesPerGroupPerSessionQueryHandler(ITrainingTemplateRepository trainingTemplateRepository)
    {
        _trainingTemplateRepository = trainingTemplateRepository;
    }

    public async Task<List<GetNumberOfSeriesPerGroupPerSessionResponse>> HandleAsync(GetNumberOfSeriesPerGroupPerSessionQuery query, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _trainingTemplateRepository.GetByIdAsync(query.TrainingTemplateId)
        ?? throw new Exception();
        List<GetNumberOfSeriesPerGroupPerSessionResponse> ret = new List<GetNumberOfSeriesPerGroupPerSessionResponse>();
        foreach(var session in trainingTemplate.TemplateSessions)
        {
            var SeriesPerGroupPerSession = session.GetNumberOfSeriesPerGroupPerSession();
            var mapped = SeriesPerGroupPerSession.ToDictionary(kv => Converters.MapMuscleGroup(kv.Key), kv => kv.Value);
            var element = new GetNumberOfSeriesPerGroupPerSessionResponse(session.Id,session.Name,mapped);
            ret.Add(element);
        }
        return ret;
    }

}