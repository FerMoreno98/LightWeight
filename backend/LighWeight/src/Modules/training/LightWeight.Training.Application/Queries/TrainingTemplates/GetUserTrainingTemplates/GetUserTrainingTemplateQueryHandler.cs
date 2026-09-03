using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.TrainingTemplates.GetUserTrainingTemplates;

public sealed class GetUserTrainingTemplatesQueryHandler : IQueryHandler<GetUserTrainingTemplatesQuery, List<GetUserTrainingTemplatesResponse>>
{
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;

    public GetUserTrainingTemplatesQueryHandler(ITrainingTemplateRepository trainingTemplateRepository)
    {
        _trainingTemplateRepository = trainingTemplateRepository;
    }

    public async Task<List<GetUserTrainingTemplatesResponse>> HandleAsync(GetUserTrainingTemplatesQuery query, CancellationToken ct = default)
    {
        List<TrainingTemplate>? trainingTemplates = await _trainingTemplateRepository.GetAllTrainingTemplatesOfAUserAsync(query.UserId)
        ?? throw new Exception();
        
        List<GetUserTrainingTemplatesResponse> ret = new List<GetUserTrainingTemplatesResponse>();
        foreach(var template in trainingTemplates)
        {
            var dictTotalVolumen = template.GetNumberOfSeriesPerGroup();
            var mapped = dictTotalVolumen.ToDictionary(dtv => Converters.MapMuscleGroup(dtv.Key), dtv => dtv.Value);
            var element = new GetUserTrainingTemplatesResponse(
                template.Id,
                template.Name,
                Converters.VolumeLandmarkConverter(template.VolumeLandmark),
                Converters.TrainingDistributionConverter(template.TrainingDistribution),
                mapped
            );
            ret.Add(element);
        }
        return ret;
    }
}