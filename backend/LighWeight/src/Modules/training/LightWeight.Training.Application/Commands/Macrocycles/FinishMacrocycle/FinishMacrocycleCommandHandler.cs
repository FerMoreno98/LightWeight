using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;

public sealed class FinishMacrocycleCommandHandler : ICommandHandler<FinishMacrocycleCommand>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IMesocycleRepository _mesocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public FinishMacrocycleCommandHandler(IMacrocycleRepository macrocycleRepository, IMesocycleRepository mesocycleRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
        _mesocycleRepository = mesocycleRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(FinishMacrocycleCommand command, CancellationToken ct = default)
    {
        Macrocycle macrocycle = await _macrocycleRepository.GetByIdAsync(command.MacrocycleId)
            ?? throw new MacrocycleNotFoundException();
        if(macrocycle.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        List<Mesocycle> mesocycles = await _mesocycleRepository.GetByMacrocycleIdAsync(macrocycle.Id);
        // The macrocycle and its active mesocycle are finished in the same SaveChanges
        macrocycle.Finish(DateTime.UtcNow, mesocycles);
        await _UOW.SaveChangesAsync(ct);
    }
}
