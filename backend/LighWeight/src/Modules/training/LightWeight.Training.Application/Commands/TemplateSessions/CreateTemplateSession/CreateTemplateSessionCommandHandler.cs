using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;

public sealed class CreateTemplateSessionCommandHandler : ICommandHandler<CreateTemplateSessionCommand, Guid>
{
    private readonly ITrainingTemplateRepository _TrainingTemplateRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTemplateSessionCommandHandler(ITrainingTemplateRepository trainingTemplateRepository, ITrainingUnitOfWork uOW)
    {
        _TrainingTemplateRepository = trainingTemplateRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateTemplateSessionCommand command, CancellationToken ct = default)
    {
        TrainingTemplate? trainingTemplate = await _TrainingTemplateRepository.GetByIdAsync(command.TrainingTemplateId)
        ?? throw new Exception();// Cambiar Cambiar Cambiar Cambiar
        if(trainingTemplate.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TemplateSession template = TemplateSession.Create
        (
            command.Name
        );
        trainingTemplate.AddSessionTemplate(template);
        await _UOW.SaveChangesAsync(ct);
        return template.Id;
    }
}