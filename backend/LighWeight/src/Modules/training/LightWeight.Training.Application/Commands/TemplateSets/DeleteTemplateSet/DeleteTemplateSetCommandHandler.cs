using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSets.DeleteTemplateSet;

public sealed class DeleteTemplateSetCommandHandler : ICommandHandler<DeleteTemplateSetCommand>
{
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public DeleteTemplateSetCommandHandler(ITrainingTemplateRepository trainingTemplateRepository, ITrainingUnitOfWork trainingUnitOfWork)
    {
        _UOW = trainingUnitOfWork;
        _trainingTemplateRepository = trainingTemplateRepository;
    }

    public async Task HandleAsync(DeleteTemplateSetCommand command, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _trainingTemplateRepository.GetByTemplateSetIdAsync(command.TemplateSetId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(command.UserId != trainingTemplate.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TemplateSession templateSession = 
            trainingTemplate.TemplateSessions
                .Single(s => s.TemplateExercises.Any(ts => ts.Id == command.TemplateSetId));
        templateSession.DeleteTemplateSet(command.TemplateSetId);
        await _UOW.SaveChangesAsync(ct);
        
    }
}