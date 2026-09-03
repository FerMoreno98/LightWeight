using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;

public sealed class CreateTemplateSetCommandHandler : ICommandHandler<CreateTemplateSetCommand>
{
    private readonly ITrainingTemplateRepository _TrainingTemplateRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTemplateSetCommandHandler(ITrainingTemplateRepository trainingTemplateRepository, ITrainingUnitOfWork uOW)
    {
        _TrainingTemplateRepository = trainingTemplateRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(CreateTemplateSetCommand command, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _TrainingTemplateRepository.GetBySessionIdAsync(command.TemplateSessionId)
        ?? throw new Exception();
        if(trainingTemplate.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TemplateSession? templateSession = trainingTemplate.TemplateSessions.SingleOrDefault(s => s.Id == command.TemplateSessionId)
        ?? throw new Exception();
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
            command.ExpectedRIR,
            aimGroups,
            trainingTechniques,
            command.SuperSetGroupId
        );
        templateSession.AddSet(set);
        await _UOW.SaveChangesAsync(ct);
        
    }
}