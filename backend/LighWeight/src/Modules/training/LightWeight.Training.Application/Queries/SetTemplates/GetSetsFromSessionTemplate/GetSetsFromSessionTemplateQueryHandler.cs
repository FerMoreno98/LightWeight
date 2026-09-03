using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
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
                MapTechnique(set.AdvanceTrainingTechniques),
                set.SuperSetGroupId,
                set.AimMuscleGroups
            );
            ret.Add(tempset);
        }
        return ret;

    }

    private static string? MapTechnique(AdvanceTrainingTechniques technique)
    {
        if (technique.IsDropSet) return "DropSet";
        if (technique.IsCluster) return "Cluster";
        if (technique.IsMyoRep) return "MyoRep";
        return null;
    }
}