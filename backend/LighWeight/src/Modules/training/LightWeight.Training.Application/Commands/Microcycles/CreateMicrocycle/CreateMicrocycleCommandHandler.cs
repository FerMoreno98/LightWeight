using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;

public sealed class CreateMicrocycleCommandHandler : ICommandHandler<CreateMicrocycleCommand>
{
    private readonly IMesocycleRepository _mesocycleRepository;
    private readonly IMicrocycleRepository _microcycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateMicrocycleCommandHandler(IMesocycleRepository mesocycleRepository, IMicrocycleRepository microcycleRepository, ITrainingUnitOfWork uOW)
    {
        _mesocycleRepository = mesocycleRepository;
        _microcycleRepository = microcycleRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(CreateMicrocycleCommand command, CancellationToken ct = default)
    {
        Mesocycle mesocycle = await _mesocycleRepository.GetByIdAsync(command.MesocycleId)
            ?? throw new MesocycleNotFoundException();

        if(mesocycle.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        var trainingDistribution = Enum.Parse<TrainingDistribution>(command.TrainingDistribution);
        Microcycle microcycle = Microcycle.Create
        (
            command.MesocycleId,
            command.UserId,
            command.DurationInDays,
            trainingDistribution
        );
        await _microcycleRepository.AddAsync(microcycle,ct);
        await _UOW.SaveChangesAsync(ct);
    }
}