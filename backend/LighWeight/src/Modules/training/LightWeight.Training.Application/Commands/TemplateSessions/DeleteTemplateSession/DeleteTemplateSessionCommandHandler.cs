using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSessions.DeleteTemplateSession;

public sealed class DeleteTemplateSessionCommandHandler : ICommandHandler<DeleteTemplateSessionCommand>
{
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public DeleteTemplateSessionCommandHandler(ITrainingTemplateRepository trainingTemplateRepository, ITrainingUnitOfWork uOW)
    {
        _trainingTemplateRepository = trainingTemplateRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(DeleteTemplateSessionCommand command, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _trainingTemplateRepository.GetBySessionIdAsync(command.SessionId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(command.UserId != trainingTemplate.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        trainingTemplate.DeleteSessionTemplate(command.SessionId);
        await _UOW.SaveChangesAsync(ct);
    }
}