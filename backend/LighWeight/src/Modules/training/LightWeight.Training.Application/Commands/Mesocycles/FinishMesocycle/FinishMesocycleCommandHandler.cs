using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;

public sealed class FinishMesocycleCommandHandler : ICommandHandler<FinishMesocycleCommand>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public FinishMesocycleCommandHandler(IMacrocycleRepository macrocycleRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(FinishMesocycleCommand command, CancellationToken ct = default)
    {
        Macrocycle macrocycle = await _macrocycleRepository.GetByMesocycleIdAsync(command.MesocycleId)
            ?? throw new MesocycleNotFoundException();
        if(macrocycle.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        // The mesocycle also finishes its active week
        macrocycle.FinishMesocycle(command.MesocycleId, DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
