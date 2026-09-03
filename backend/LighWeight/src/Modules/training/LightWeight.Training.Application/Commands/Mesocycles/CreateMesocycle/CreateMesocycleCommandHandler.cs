using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

public sealed class CreateMesocycleCommandHandler : ICommandHandler<CreateMesocycleCommand>
{
    private readonly IMesocycleRepository _mesocycleRepository;
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateMesocycleCommandHandler(IMesocycleRepository mesocycleRepository, IMacrocycleRepository macrocycleRepository, ITrainingUnitOfWork uOW)
    {
        _mesocycleRepository = mesocycleRepository;
        _macrocycleRepository = macrocycleRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(CreateMesocycleCommand command, CancellationToken ct = default)
    {
        Macrocycle? macrocycle = await _macrocycleRepository.GetByIdAsync(command.MacrocycleId)
        ?? throw new MacrocycleNotFoundException();
        if (macrocycle.UserId != command.UserId) throw new UnauthorizedAccessException();
        List<MuscleGroups> AimMuscleGroups = new List<MuscleGroups>();
        foreach(var Muscle in command.AimMuscles)
        {
            var muscle = Enum.Parse<MuscleGroups>(Muscle);
            AimMuscleGroups.Add(muscle);
        }
        Mesocycle mesocycle = Mesocycle.Create
        (
            command.MacrocycleId,
            macrocycle.UserId,
            AimMuscleGroups,
            command.MotivationLevel,
            command.Injuries,
            command.Comments,
            command.StartAt,
            command.EndAt
        );
        await _mesocycleRepository.AddAsync(mesocycle,ct);
        await _UOW.SaveChangesAsync(ct);
    }
}