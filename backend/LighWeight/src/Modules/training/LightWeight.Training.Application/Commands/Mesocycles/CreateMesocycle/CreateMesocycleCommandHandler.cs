using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

public sealed class CreateMesocycleCommandHandler : ICommandHandler<CreateMesocycleCommand>
{
    private readonly IMesocycleRepository _mesocycleRepository;
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateMesocycleCommandHandler(IMesocycleRepository mesocycleRepository, IMacrocycleRepository macrocycleRepository, IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _mesocycleRepository = mesocycleRepository;
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(CreateMesocycleCommand command, CancellationToken ct = default)
    {
        Macrocycle? macrocycle = await _macrocycleRepository.GetByIdAsync(command.MacrocycleId)
        ?? throw new MacrocycleNotFoundException();
        if (macrocycle.UserId != command.UserId) throw new UnauthorizedAccessException();
        Program? program = await _programRepository.GetByIdAsync(command.ProgramId)
        ?? throw new ProgramNotFoundApplicationException();
        if (program.UserId != command.UserId) throw new UnauthorizedAccessException();
        Mesocycle mesocycle = Mesocycle.Create
        (
            command.MacrocycleId,
            macrocycle.UserId,
            command.MotivationLevel,
            command.Injuries,
            command.Comments,
            command.StartAt,
            command.EndAt,
            command.ProgramId
        );
        await _mesocycleRepository.AddAsync(mesocycle,ct);
        await _UOW.SaveChangesAsync(ct);
    }
}
