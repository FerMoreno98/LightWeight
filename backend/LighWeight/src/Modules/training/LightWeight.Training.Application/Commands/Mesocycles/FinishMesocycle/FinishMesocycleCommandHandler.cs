using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;

public sealed class FinishMesocycleCommandHandler : ICommandHandler<FinishMesocycleCommand>
{
    private readonly IMesocycleRepository _mesocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public FinishMesocycleCommandHandler(IMesocycleRepository mesocycleRepository, ITrainingUnitOfWork uOW)
    {
        _mesocycleRepository = mesocycleRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(FinishMesocycleCommand command, CancellationToken ct = default)
    {
        Mesocycle mesocycle = await _mesocycleRepository.GetByIdAsync(command.MesocycleId)
            ?? throw new MesocycleNotFoundException();
        if(mesocycle.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        mesocycle.Finish(DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
