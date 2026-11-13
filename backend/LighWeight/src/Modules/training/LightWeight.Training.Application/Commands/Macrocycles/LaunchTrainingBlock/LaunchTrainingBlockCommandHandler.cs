using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Macrocycles.LaunchTrainingBlock;

public sealed class LaunchTrainingBlockCommandHandler : ICommandHandler<LaunchTrainingBlockCommand, LaunchTrainingBlockResponse>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public LaunchTrainingBlockCommandHandler(IMacrocycleRepository macrocycleRepository, IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<LaunchTrainingBlockResponse> HandleAsync(LaunchTrainingBlockCommand command, CancellationToken ct = default)
    {
        var stage = Enum.Parse<TrainingStage>(command.TrainingStage, ignoreCase: true);

        Program program = await _programRepository.GetByIdAsync(command.ProgramId)
            ?? throw new ProgramNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        // The first week must follow a template of the program the mesocycle follows
        if(!program.trainingTemplates.Any(t => t.Id == command.TrainingTemplateId))
        {
            throw new TrainingTemplateNotFoundApplicationException();
        }

        // The domain refuses to start a macrocycle while another one is active
        Macrocycle? activeMacrocycle = await _macrocycleRepository.GetActiveOfAUserAsync(command.UserId);
        // The three levels share the same start timestamp
        DateTime now = DateTime.UtcNow;
        Macrocycle macrocycle = Macrocycle.Create(command.UserId, stage, command.MacrocycleComments, now, activeMacrocycle);
        Mesocycle mesocycle = macrocycle.PlanMesocycle
        (
            command.ProgramId,
            command.MotivationLevel,
            command.Injuries,
            command.MesocycleComments,
            now
        );
        Microcycle microcycle = macrocycle.PlanMicrocycle(mesocycle.Id, command.TrainingTemplateId, now);

        await _macrocycleRepository.AddAsync(macrocycle, ct);
        await _UOW.SaveChangesAsync(ct);
        return new LaunchTrainingBlockResponse(macrocycle.Id, mesocycle.Id, microcycle.Id);
    }
}
