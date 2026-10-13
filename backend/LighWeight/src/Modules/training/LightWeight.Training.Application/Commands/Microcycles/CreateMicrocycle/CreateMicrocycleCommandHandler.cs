using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;

public sealed class CreateMicrocycleCommandHandler : ICommandHandler<CreateMicrocycleCommand, Guid>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateMicrocycleCommandHandler(IMacrocycleRepository macrocycleRepository, IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateMicrocycleCommand command, CancellationToken ct = default)
    {
        Macrocycle macrocycle = await _macrocycleRepository.GetByMesocycleIdAsync(command.MesocycleId)
            ?? throw new MesocycleNotFoundException();

        if(macrocycle.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        Mesocycle mesocycle = macrocycle.Mesocycles.Single(m => m.Id == command.MesocycleId);
        // The template must belong to the program the mesocycle follows
        Program program = await _programRepository.GetByIdAsync(mesocycle.ProgramId)
            ?? throw new ProgramNotFoundApplicationException();
        if(!program.trainingTemplates.Any(t => t.Id == command.TrainingTemplateId))
        {
            throw new TrainingTemplateNotFoundApplicationException();
        }
        // The mesocycle checks it is still active, has no active week and numbers the new one
        Microcycle microcycle = macrocycle.PlanMicrocycle(command.MesocycleId, command.TrainingTemplateId, DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
        return microcycle.Id;
    }
}
