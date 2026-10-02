using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DeleteTrainingTemplate;

public sealed class DeleteTrainingTemplateCommandHandler : ICommandHandler<DeleteTrainingTemplateCommand>
{
    private readonly ITrainingUnitOfWork _UOW;
    private readonly IProgramRepository _programRepository;

    public DeleteTrainingTemplateCommandHandler(ITrainingUnitOfWork uOW, IProgramRepository programRepository)
    {
        _UOW = uOW;
        _programRepository = programRepository;
    }

    public async Task HandleAsync(DeleteTrainingTemplateCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(command.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(command.UserId != program.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        program.DeleteTrainingTemplate(command.TrainingTemplateId, DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
