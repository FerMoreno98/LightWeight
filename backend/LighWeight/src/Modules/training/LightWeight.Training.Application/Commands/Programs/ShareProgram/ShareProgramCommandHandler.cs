using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.Programs.ShareProgram;

public sealed class ShareProgramCommandHandler : ICommandHandler<ShareProgramCommand, string>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public ShareProgramCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<string> HandleAsync(ShareProgramCommand command, CancellationToken ct = default)
    {
        Program program = await _programRepository.GetByIdAsync(command.ProgramId)
            ?? throw new ProgramNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        string token = program.Share();
        await _UOW.SaveChangesAsync(ct);
        return token;
    }
}
