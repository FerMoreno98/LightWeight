using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Commands.TrainingSessions.RegisterSet;

public sealed class RegisterSetCommandHandler : ICommandHandler<RegisterSetCommand, Guid>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IProgramRepository _programRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public RegisterSetCommandHandler
    (
        ITrainingSessionRepository trainingSessionRepository,
        IProgramRepository programRepository,
        IExerciseRepository exerciseRepository,
        ITrainingUnitOfWork uOW
    )
    {
        _trainingSessionRepository = trainingSessionRepository;
        _programRepository = programRepository;
        _exerciseRepository = exerciseRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(RegisterSetCommand command, CancellationToken ct = default)
    {
        TrainingSession trainingSession = await _trainingSessionRepository.GetByIdAsync(command.TrainingSessionId)
            ?? throw new TrainingSessionNotFoundApplicationException();
        if(trainingSession.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        // The exercise performed may be any of the catalog: it can replace the planned one
        _ = await _exerciseRepository.GetByIdAsync(command.ExerciseId)
            ?? throw new ExerciseNotFounApplicationException();

        AdvanceTrainingTechniques techniques = AdvanceTrainingTechniques.None;
        if(command.TemplateSetId is not null)
        {
            // The planned set must be one of the session of the template this session follows
            TemplateSet templateSet = await GetPlannedSetAsync(trainingSession, command.TemplateSetId.Value);
            techniques = templateSet.AdvanceTrainingTechniques ?? AdvanceTrainingTechniques.None;
        }

        Set set = trainingSession.RegisterSet
        (
            command.ExerciseId,
            command.TemplateSetId,
            techniques,
            command.SuperSetGroupId,
            command.Repetitions,
            command.Weight,
            command.RPE,
            command.IsBodyWeight
        );
        await _UOW.SaveChangesAsync(ct);
        return set.Id;
    }

    private async Task<TemplateSet> GetPlannedSetAsync(TrainingSession trainingSession, Guid templateSetId)
    {
        if(trainingSession.TemplateSessionId is not Guid templateSessionId)
        {
            throw new TemplateSetNotFoundApplicationException();
        }
        Program? program = await _programRepository.GetByTemplateSessionIdAsync(templateSessionId);
        return program?.trainingTemplates
            .SelectMany(t => t.TemplateSessions)
            .SingleOrDefault(s => s.Id == templateSessionId)?
            .TemplateExercises.SingleOrDefault(s => s.Id == templateSetId)
            ?? throw new TemplateSetNotFoundApplicationException();
    }
}
