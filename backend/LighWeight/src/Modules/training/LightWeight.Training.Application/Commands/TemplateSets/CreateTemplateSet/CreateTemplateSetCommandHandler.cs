using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;

public sealed class CreateTemplateSetCommandHandler : ICommandHandler<CreateTemplateSetCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly IExerciseRepository _ExerciseRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTemplateSetCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW,IExerciseRepository exerciseRepository)
    {
        _programRepository = programRepository;
        _ExerciseRepository = exerciseRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(CreateTemplateSetCommand command, CancellationToken ct = default)
    {
        Exercise? exercise = await _ExerciseRepository.GetByIdAsync(command.ExerciseId)
        ?? throw new ExerciseNotFounApplicationException();
        Program? program = await _programRepository.GetByTemplateSessionIdAsync(command.TemplateSessionId)
        ?? throw new TemplateSessionNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TemplateSession? templateSession = program.trainingTemplates
            .SelectMany(t => t.TemplateSessions)
            .SingleOrDefault(s => s.Id == command.TemplateSessionId)
        ?? throw new TemplateSessionNotFoundApplicationException();
        for(var i=0; i < command.Series; i++)
        {
        RepetitionRange range = RepetitionRange.Create(command.Min, command.Max);
        AdvanceTrainingTechniques trainingTechniques = AdvanceTrainingTechniques.Create
        (
            command.IsDropSet,
            command.IsCluster,
            command.IsMyoRep      

        );
        List<MuscleGroups> aimGroups = new List<MuscleGroups>();
        foreach(var muscleGroup in command.AimMuscleGroups)
        {
            var muscle = Enum.Parse<MuscleGroups>(muscleGroup);
            aimGroups.Add(muscle);
        }
        TemplateSet set = TemplateSet.Create
        (
            command.ExerciseId,
            range,
            command.ExpectedRPE,
            aimGroups,
            trainingTechniques,
            command.SuperSetGroupId
        );
        templateSession.AddSet(set);
        }
        await _UOW.SaveChangesAsync(ct);
        
    }
}