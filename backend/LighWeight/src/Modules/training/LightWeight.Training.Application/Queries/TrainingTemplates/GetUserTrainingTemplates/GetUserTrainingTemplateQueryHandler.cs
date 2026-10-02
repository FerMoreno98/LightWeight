using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.TrainingTemplates.GetUserTrainingTemplates;

public sealed class GetUserTrainingTemplatesQueryHandler : IQueryHandler<GetUserTrainingTemplatesQuery, List<GetUserTrainingTemplatesResponse>>
{
    private readonly IProgramRepository _programRepository;

    public GetUserTrainingTemplatesQueryHandler(IProgramRepository programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<List<GetUserTrainingTemplatesResponse>> HandleAsync(GetUserTrainingTemplatesQuery query, CancellationToken ct = default)
    {
        List<Program> programs = await _programRepository.GetAllProgramsOfAUserAsync(query.UserId);
        
        List<GetUserTrainingTemplatesResponse> ret = new List<GetUserTrainingTemplatesResponse>();
        foreach(var program in programs)
        foreach(var template in program.trainingTemplates.OrderBy(t => t.Order))
        {
            var dictTotalVolumen = template.GetNumberOfSeriesPerGroup();
            var mapped = dictTotalVolumen.ToDictionary(dtv => Converters.MapMuscleGroup(dtv.Key), dtv => dtv.Value);
            var element = new GetUserTrainingTemplatesResponse(
                template.Id,
                program.Id,
                program.Name,
                template.Order,
                template.DurationInDays,
                Converters.VolumeLandmarkConverter(template.VolumeLandmark),
                Converters.TrainingDistributionConverter(template.TrainingDistribution),
                mapped
            );
            ret.Add(element);
        }
        return ret;
    }
}