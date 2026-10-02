using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;

public sealed class GetSetsFromSessionTemplateQueryHandler : IQueryHandler<GetSetsFromSessionTemplateQuery, List<GetSetsFromSessionTemplateResponse>>
{
    private readonly IProgramRepository _programRepository;

    public GetSetsFromSessionTemplateQueryHandler(IProgramRepository programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<List<GetSetsFromSessionTemplateResponse>> HandleAsync(GetSetsFromSessionTemplateQuery query, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(query.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        TrainingTemplate? trainingTemplate = program.trainingTemplates.SingleOrDefault(t => t.Id == query.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        TemplateSession? session = trainingTemplate.TemplateSessions.SingleOrDefault(ts => ts.Id == query.TemplateSessionId)
        ?? throw new TemplateSessionNotFoundApplicationException();
        IReadOnlyCollection<TemplateSet> templateSets = session.TemplateExercises;

        List<GetSetsFromSessionTemplateResponse> ret = new List<GetSetsFromSessionTemplateResponse>();
        foreach(var set in templateSets)
        {
            var tempset = new GetSetsFromSessionTemplateResponse
            (
                set.Id,
                set.ExerciseId,
                set.RepetitionRange.Min,
                set.RepetitionRange.Max,
                set.ExpectedRPE,
                Converters.MapTechnique(set.AdvanceTrainingTechniques),
                set.SuperSetGroupId,
                set.AimMuscleGroups.Select(Converters.MapMuscleGroup).ToList()
            );
            ret.Add(tempset);
        }
        return ret;

    }


}