using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

public sealed class CreateMesocycleCommandHandler : ICommandHandler<CreateMesocycleCommand, Guid>
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

    public async Task<Guid> HandleAsync(CreateMesocycleCommand command, CancellationToken ct = default)
    {
        Macrocycle? macrocycle = await _macrocycleRepository.GetByIdAsync(command.MacrocycleId)
        ?? throw new MacrocycleNotFoundException();
        if (macrocycle.UserId != command.UserId) throw new UnauthorizedAccessException();
        Program? program = await _programRepository.GetByIdAsync(command.ProgramId)
        ?? throw new ProgramNotFoundApplicationException();
        if (program.UserId != command.UserId) throw new UnauthorizedAccessException();
        // The macrocycle checks it is still active and has no active mesocycle
        List<Mesocycle> mesocycles = await _mesocycleRepository.GetByMacrocycleIdAsync(macrocycle.Id);
        Mesocycle mesocycle = macrocycle.PlanMesocycle
        (
            mesocycles,
            command.ProgramId,
            command.MotivationLevel,
            command.Injuries,
            command.Comments,
            DateTime.UtcNow
        );
        await _mesocycleRepository.AddAsync(mesocycle,ct);
        await _UOW.SaveChangesAsync(ct);
        return mesocycle.Id;
    }
}
