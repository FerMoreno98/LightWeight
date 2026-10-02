using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.SessionTemplates.GetNumberOfSeriesPerGroupPerSession;

public sealed class GetNumberOfSeriesPerGroupPerSessionQueryHandler : IQueryHandler<GetNumberOfSeriesPerGroupPerSessionQuery, List<GetNumberOfSeriesPerGroupPerSessionResponse>>
{
    private readonly IProgramRepository _programRepository;

    public GetNumberOfSeriesPerGroupPerSessionQueryHandler(IProgramRepository programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<List<GetNumberOfSeriesPerGroupPerSessionResponse>> HandleAsync(GetNumberOfSeriesPerGroupPerSessionQuery query, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(query.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        TrainingTemplate? trainingTemplate = program.trainingTemplates.SingleOrDefault(t => t.Id == query.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
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