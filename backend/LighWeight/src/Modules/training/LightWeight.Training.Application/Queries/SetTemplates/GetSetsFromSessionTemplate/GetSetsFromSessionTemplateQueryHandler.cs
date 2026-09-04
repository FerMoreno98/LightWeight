using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;

public sealed class GetSetsFromSessionTemplateQueryHandler : IQueryHandler<GetSetsFromSessionTemplateQuery, List<GetSetsFromSessionTemplateResponse>>
{
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;

    public GetSetsFromSessionTemplateQueryHandler(ITrainingTemplateRepository trainingTemplateRepository)
    {
        _trainingTemplateRepository = trainingTemplateRepository;
    }

    public async Task<List<GetSetsFromSessionTemplateResponse>> HandleAsync(GetSetsFromSessionTemplateQuery query, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _trainingTemplateRepository.GetByIdAsync(query.TrainingTemplateId)
        ?? throw new Exception();
        TemplateSession? session = trainingTemplate.TemplateSessions.SingleOrDefault(ts => ts.Id == query.TemplateSessionId)
        ?? throw new Exception();
        IReadOnlyCollection<TemplateSet> templateSets = session.TemplateExercises;

        List<GetSetsFromSessionTemplateResponse> ret = new List<GetSetsFromSessionTemplateResponse>();
        foreach(var set in templateSets)
        {
            var tempset = new GetSetsFromSessionTemplateResponse
            (
                set.ExerciseId,
                set.RepetitionRange.Min,
                set.RepetitionRange.Max,
                set.ExpectedRIR,
                Converters.MapTechnique(set.AdvanceTrainingTechniques),
                set.SuperSetGroupId,
                set.AimMuscleGroups.Select(Converters.MapMuscleGroup).ToList()
            );
            ret.Add(tempset);
        }
        return ret;

    }


}