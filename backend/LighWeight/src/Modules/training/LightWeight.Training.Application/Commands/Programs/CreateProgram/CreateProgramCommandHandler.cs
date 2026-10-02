using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Programs.CreateProgram;

public sealed class CreateProgramCommandHandler : ICommandHandler<CreateProgramCommand, Guid>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateProgramCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateProgramCommand command, CancellationToken ct = default)
    {
        var periodization = Enum.Parse<Periodization>(command.Periodization);
        List<MuscleGroups> aimMuscleGroups = new List<MuscleGroups>();
        foreach(var muscleGroup in command.AimMuscleGroups)
        {
            aimMuscleGroups.Add(Enum.Parse<MuscleGroups>(muscleGroup));
        }
        Program program = Program.Create
        (
            command.UserId,
            periodization,
            command.Name,
            aimMuscleGroups
        );
        await _programRepository.AddAsync(program, ct);
        await _UOW.SaveChangesAsync(ct);
        return program.Id;
    }
}
