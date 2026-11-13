using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Setup.GetTrainingSetupStatus;

public sealed class GetTrainingSetupStatusQueryHandler : IQueryHandler<GetTrainingSetupStatusQuery, GetTrainingSetupStatusResponse>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;

    public GetTrainingSetupStatusQueryHandler(IMacrocycleRepository macrocycleRepository, IProgramRepository programRepository)
    {
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
    }

    public async Task<GetTrainingSetupStatusResponse> HandleAsync(GetTrainingSetupStatusQuery query, CancellationToken ct = default)
    {
        List<Macrocycle> macrocycles = await _macrocycleRepository.GetAllOfAUserAsync(query.UserId);
        bool isFirstTime = macrocycles.Count == 0;
        bool isTraining = macrocycles.Any(m => !m.IsFinished);

        Program? program = await GetProgramAsync(query);
        if(program is null)
        {
            string step = isTraining ? TrainingSetupStep.Done : TrainingSetupStep.CreateProgram;
            return new GetTrainingSetupStatusResponse(step, false, isFirstTime, null, null, new());
        }

        List<TrainingTemplate> templates = program.trainingTemplates.OrderBy(t => t.Order).ToList();
        List<SetupTemplateResponse> templateResponses = templates
            .Select(t => new SetupTemplateResponse
            (
                t.Id,
                t.Name,
                Converters.VolumeLandmarkConverter(t.VolumeLandmark),
                t.TemplateSessions.Count,
                t.TemplateSessions.Count(s => s.TemplateExercises.Count > 0)
            ))
            .ToList();

        // The base template is the first one: the rest are copies of it at other volume levels
        SetupTemplateResponse? baseTemplate = templateResponses.FirstOrDefault();
        bool canLaunch = baseTemplate is not null
            && baseTemplate.Sessions > 0
            && baseTemplate.SessionsWithSets == baseTemplate.Sessions;

        string nextStep = isTraining ? TrainingSetupStep.Done
            : baseTemplate is null ? TrainingSetupStep.CreateTemplate
            : !canLaunch ? TrainingSetupStep.FillSessions
            : templateResponses.Count == 1 ? TrainingSetupStep.AddVolumeLevels
            : TrainingSetupStep.LaunchBlock;

        return new GetTrainingSetupStatusResponse
        (
            nextStep,
            canLaunch && !isTraining,
            isFirstTime,
            program.Id,
            program.Name,
            templateResponses
        );
    }

    private async Task<Program?> GetProgramAsync(GetTrainingSetupStatusQuery query)
    {
        if(query.ProgramId is Guid programId)
        {
            Program program = await _programRepository.GetByIdAsync(programId)
                ?? throw new ProgramNotFoundApplicationException();
            if(program.UserId != query.UserId)
            {
                throw new UnauthorizedAccessException();
            }
            return program;
        }
        // Without a program, the most recent one (version 7 ids sort by creation time)
        List<Program> programs = await _programRepository.GetAllProgramsOfAUserAsync(query.UserId);
        return programs.MaxBy(p => p.Id);
    }
}
