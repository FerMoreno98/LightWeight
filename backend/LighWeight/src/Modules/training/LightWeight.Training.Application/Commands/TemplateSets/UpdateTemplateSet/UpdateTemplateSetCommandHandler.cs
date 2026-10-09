using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Commands.TemplateSets.UpdateTemplateSet;

public sealed class UpdateTemplateSetCommandHandler : ICommandHandler<UpdateTemplateSetCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public UpdateTemplateSetCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(UpdateTemplateSetCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTemplateSessionIdAsync(command.TemplateSessionId)
        ?? throw new TemplateSessionNotFoundApplicationException();
        if(command.UserId != program.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TemplateSession? templateSession = program.trainingTemplates
            .SelectMany(t => t.TemplateSessions)
            .SingleOrDefault(ts => ts.Id == command.TemplateSessionId)
                ?? throw new TemplateSessionNotFoundApplicationException();

        TemplateSet? templateSet = templateSession
            .TemplateExercises.SingleOrDefault(ts => ts.Id == command.SetId)
                ?? throw new TemplateSetNotFoundApplicationException();
        
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
            var muscle = Enum.Parse<MuscleGroups>(muscleGroup, ignoreCase: true);
            aimGroups.Add(muscle);
        }
        
        templateSet.UpdateSet
        (
            range,
            command.ExpectedRPE,
            aimGroups,
            trainingTechniques,
            command.SuperSetGroupId

        );
        await _UOW.SaveChangesAsync(ct);
                                    
    }
}