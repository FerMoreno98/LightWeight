using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Programs.RevokeProgramShare;

public sealed class RevokeProgramShareCommandHandler : ICommandHandler<RevokeProgramShareCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public RevokeProgramShareCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(RevokeProgramShareCommand command, CancellationToken ct = default)
    {
        Program program = await _programRepository.GetByIdAsync(command.ProgramId)
            ?? throw new ProgramNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        program.RevokeShare();
        await _UOW.SaveChangesAsync(ct);
    }
}
