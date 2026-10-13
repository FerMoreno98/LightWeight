using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Microcycles.FinishMicrocycle;

public sealed class FinishMicrocycleCommandHandler : ICommandHandler<FinishMicrocycleCommand>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public FinishMicrocycleCommandHandler(IMacrocycleRepository macrocycleRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(FinishMicrocycleCommand command, CancellationToken ct = default)
    {
        Macrocycle macrocycle = await _macrocycleRepository.GetByMicrocycleIdAsync(command.MicrocycleId)
            ?? throw new MicrocycleNotFoundException();
        if(macrocycle.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        macrocycle.FinishMicrocycle(command.MicrocycleId, DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
