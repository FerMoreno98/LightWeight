using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DeleteTrainingTemplate;

public sealed class DeleteTrainingTemplateCommandHandler : ICommandHandler<DeleteTrainingTemplateCommand>
{
    private readonly ITrainingUnitOfWork _UOW;
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;

    public DeleteTrainingTemplateCommandHandler(ITrainingUnitOfWork uOW, ITrainingTemplateRepository trainingTemplateRepository)
    {
        _UOW = uOW;
        _trainingTemplateRepository = trainingTemplateRepository;
    }

    public async Task HandleAsync(DeleteTrainingTemplateCommand command, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _trainingTemplateRepository.GetByIdAsync(command.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(command.UserId != trainingTemplate.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        await _trainingTemplateRepository.DeleteAsync(trainingTemplate, ct);
    }
}