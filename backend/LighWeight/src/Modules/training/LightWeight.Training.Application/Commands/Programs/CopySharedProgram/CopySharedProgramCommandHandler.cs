using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Programs.CopySharedProgram;

/// <summary>
/// Any authenticated user with the link can copy it (the author too). The token is not consumed.
/// When custom exercises exist, this is the place to also copy them and remap the ExerciseIds
/// </summary>
public sealed class CopySharedProgramCommandHandler : ICommandHandler<CopySharedProgramCommand, Guid>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CopySharedProgramCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CopySharedProgramCommand command, CancellationToken ct = default)
    {
        Program original = await _programRepository.GetByShareTokenAsync(command.ShareToken)
            ?? throw new SharedProgramNotFoundApplicationException();
        Program copy = original.CopyFor(command.UserId);
        await _programRepository.AddAsync(copy, ct);
        await _UOW.SaveChangesAsync(ct);
        return copy.Id;
    }
}
