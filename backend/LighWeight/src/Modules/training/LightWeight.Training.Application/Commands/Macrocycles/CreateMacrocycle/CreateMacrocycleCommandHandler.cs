using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

public sealed class CreateMacrocycleCommandHandler : ICommandHandler<CreateMacrocycleCommand, Guid>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateMacrocycleCommandHandler(IMacrocycleRepository macrocycleRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateMacrocycleCommand command, CancellationToken ct = default)
    {
        var stage = Enum.Parse<TrainingStage>(command.TrainingStage, ignoreCase: true);
        // The domain refuses to start a macrocycle while another one is active
        Macrocycle? activeMacrocycle = await _macrocycleRepository.GetActiveOfAUserAsync(command.UserId);
        Macrocycle macrocycle = Macrocycle.Create
        (
            command.UserId,
            stage,
            command.Comments,
            DateTime.UtcNow,
            activeMacrocycle
        );
        await _macrocycleRepository.AddAsync(macrocycle,ct);
        await _UOW.SaveChangesAsync(ct);
        return macrocycle.Id;
    }
}
