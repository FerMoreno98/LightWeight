using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;

public sealed class GetSetsFromSessionTemplateQueryHandler : IQueryHandler<GetSetsFromSessionTemplateQuery, List<GetSetsFromSessionTemplateResponse>>
{
    private readonly IProgramRepository _programRepository;
    private readonly IExerciseRepository _exerciseRepository;

    public GetSetsFromSessionTemplateQueryHandler(IProgramRepository programRepository, IExerciseRepository exerciseRepository)
    {
        _programRepository = programRepository;
        _exerciseRepository = exerciseRepository;
    }

    public async Task<List<GetSetsFromSessionTemplateResponse>> HandleAsync(GetSetsFromSessionTemplateQuery query, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(query.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(program.UserId != query.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TrainingTemplate? trainingTemplate = program.trainingTemplates.SingleOrDefault(t => t.Id == query.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        TemplateSession? session = trainingTemplate.TemplateSessions.SingleOrDefault(ts => ts.Id == query.TemplateSessionId)
        ?? throw new TemplateSessionNotFoundApplicationException();
        IReadOnlyCollection<TemplateSet> templateSets = session.TemplateExercises;

        // One query for the names of every exercise of the session
        Dictionary<Guid, string> exerciseNames = (await _exerciseRepository.GetByIdsAsync(templateSets.Select(s => s.ExerciseId)))
            .ToDictionary(e => e.Id, e => e.Name);

        List<GetSetsFromSessionTemplateResponse> ret = new List<GetSetsFromSessionTemplateResponse>();
        foreach(var set in templateSets)
        {
            var tempset = new GetSetsFromSessionTemplateResponse
            (
                set.Id,
                set.ExerciseId,
                exerciseNames.GetValueOrDefault(set.ExerciseId, string.Empty),
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
