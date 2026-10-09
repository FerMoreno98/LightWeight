using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.RenameTrainingTemplate;

public sealed class RenameTrainingTemplateCommandHandler : ICommandHandler<RenameTrainingTemplateCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public RenameTrainingTemplateCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(RenameTrainingTemplateCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(command.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(command.UserId != program.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        program.RenameTrainingTemplate(command.TrainingTemplateId, command.Name);
        await _UOW.SaveChangesAsync(ct);
    }
}
