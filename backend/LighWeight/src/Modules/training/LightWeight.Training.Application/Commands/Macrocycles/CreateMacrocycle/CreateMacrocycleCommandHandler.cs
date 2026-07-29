using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

public sealed class CreateMacrocycleCommandHandler : ICommandHandler<CreateMacrocycleCommand>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateMacrocycleCommandHandler(IMacrocycleRepository macrocycleRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(CreateMacrocycleCommand command, CancellationToken ct = default)
    {
        var Stage = Enum.Parse<TrainingStage>(command.TrainingStage);
        var periodization = Enum.Parse<Periodization>(command.Periodization);
        Macrocycle macrocycle = Macrocycle.Create
        (
            command.UserId,
            command.StartAt,
            command.EndAt,
            Stage,
            periodization,
            command.Comments
        );
        await _macrocycleRepository.AddAsync(macrocycle,ct);
        await _UOW.SaveChangesAsync(ct);
    }
}

