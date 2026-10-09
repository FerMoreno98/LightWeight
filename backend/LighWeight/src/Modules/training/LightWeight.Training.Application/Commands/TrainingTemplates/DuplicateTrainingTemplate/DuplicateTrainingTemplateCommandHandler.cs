using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;

public sealed class DuplicateTrainingTemplateCommandHandler : ICommandHandler<DuplicateTrainingTemplateCommand, Guid>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public DuplicateTrainingTemplateCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(DuplicateTrainingTemplateCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(command.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(command.UserId != program.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TrainingTemplate copy = program.DuplicateTrainingTemplate(command.TrainingTemplateId);
        await _UOW.SaveChangesAsync(ct);
        return copy.Id;
    }
}
