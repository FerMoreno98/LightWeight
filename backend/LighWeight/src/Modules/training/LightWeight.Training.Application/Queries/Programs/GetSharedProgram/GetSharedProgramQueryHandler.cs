using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Projections.UserNames;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Programs.GetSharedProgram;

/// <summary>
/// The only query that reads a program of another user: it is reached through the share token, never through the id,
/// and it exposes no ids of the program or its author
/// </summary>
public sealed class GetSharedProgramQueryHandler : IQueryHandler<GetSharedProgramQuery, GetSharedProgramResponse>
{
    private readonly IProgramRepository _programRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IUserNameProjectionStore _userNames;

    public GetSharedProgramQueryHandler
    (
        IProgramRepository programRepository,
        IExerciseRepository exerciseRepository,
        IUserNameProjectionStore userNames
    )
    {
        _programRepository = programRepository;
        _exerciseRepository = exerciseRepository;
        _userNames = userNames;
    }

    public async Task<GetSharedProgramResponse> HandleAsync(GetSharedProgramQuery query, CancellationToken ct = default)
    {
        Program program = await _programRepository.GetByShareTokenAsync(query.ShareToken)
            ?? throw new SharedProgramNotFoundApplicationException();

        // One query for the names of every exercise of the program
        List<Guid> exerciseIds = program.trainingTemplates
            .SelectMany(t => t.TemplateSessions)
            .SelectMany(s => s.TemplateExercises)
            .Select(s => s.ExerciseId)
            .Distinct()
            .ToList();
        Dictionary<Guid, string> exerciseNames = (await _exerciseRepository.GetByIdsAsync(exerciseIds))
            .ToDictionary(e => e.Id, e => e.Name);

        string? authorName = await _userNames.GetNameAsync(program.UserId, ct);

        return new GetSharedProgramResponse
        (
            program.Name,
            program.Periodization.ToString(),
            program.AimMuscleGroups.Select(Converters.MapMuscleGroup).ToList(),
            authorName,
            program.trainingTemplates
                .OrderBy(t => t.Order)
                .Select(t => new GetSharedProgramTemplateResponse
                (
                    t.Name,
                    t.DurationInDays,
                    Converters.VolumeLandmarkConverter(t.VolumeLandmark),
                    Converters.TrainingDistributionConverter(t.TrainingDistribution),
                    t.TemplateSessions
                        .Select(s => new GetSharedProgramSessionResponse
                        (
                            s.Name,
                            s.TemplateExercises
                                .Select(set => new GetSharedProgramSetResponse
                                (
                                    exerciseNames.GetValueOrDefault(set.ExerciseId, string.Empty),
                                    set.RepetitionRange.Min,
                                    set.RepetitionRange.Max,
                                    set.ExpectedRPE,
                                    Converters.MapTechnique(set.AdvanceTrainingTechniques),
                                    set.SuperSetGroupId,
                                    set.AimMuscleGroups.Select(Converters.MapMuscleGroup).ToList()
                                ))
                                .ToList()
                        ))
                        .ToList()
                ))
                .ToList()
        );
    }
}
