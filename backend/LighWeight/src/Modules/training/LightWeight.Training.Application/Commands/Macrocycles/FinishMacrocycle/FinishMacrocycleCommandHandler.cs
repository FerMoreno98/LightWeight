using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;

public sealed class FinishMacrocycleCommandHandler : ICommandHandler<FinishMacrocycleCommand>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public FinishMacrocycleCommandHandler(IMacrocycleRepository macrocycleRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
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
        // The macrocycle also finishes its active mesocycle and active week
        macrocycle.Finish(DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
