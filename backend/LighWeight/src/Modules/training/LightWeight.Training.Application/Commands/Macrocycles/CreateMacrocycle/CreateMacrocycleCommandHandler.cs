using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

public sealed class CreateMacrocycleCommandHandler : ICommandHandler<CreateMacrocycleCommand>
{
    private readonly IMacrocycleRepository _macrocycleRepository;

    public CreateMacrocycleCommandHandler(IMacrocycleRepository macrocycleRepository)
    {
        _macrocycleRepository = macrocycleRepository;
    }

    public async Task HandleAsync(CreateMacrocycleCommand command, CancellationToken ct = default)
    {
        List<MuscleGroups> muscleGroups = new List<MuscleGroups>();
        foreach(var muscle in command.AimMuscleGroups)
        {
            var muscleGroup = Enum.Parse<MuscleGroups>(muscle);
            muscleGroups.Add(muscleGroup);
        }
        var Stage = Enum.Parse<TrainingStage>(command.TrainingStage);
        var periodization = Enum.Parse<Periodization>(command.Periodization);
        Macrocycle macrocycle = Macrocycle.Create
        (
            command.UserId,
            muscleGroups,
            command.StartAt,
            command.EndAt,
            Stage,
            periodization,
            command.Comments
        );
        await _macrocycleRepository.AddAsync(macrocycle,ct);
    }
}

