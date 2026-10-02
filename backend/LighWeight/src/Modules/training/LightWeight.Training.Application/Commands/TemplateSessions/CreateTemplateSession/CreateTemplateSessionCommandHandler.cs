using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;

public sealed class CreateTemplateSessionCommandHandler : ICommandHandler<CreateTemplateSessionCommand, Guid>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTemplateSessionCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateTemplateSessionCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(command.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TrainingTemplate? trainingTemplate = program.trainingTemplates.SingleOrDefault(t => t.Id == command.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        TemplateSession template = TemplateSession.Create
        (
            command.Name
        );
        trainingTemplate.AddSessionTemplate(template);
        await _UOW.SaveChangesAsync(ct);
        return template.Id;
    }
}
