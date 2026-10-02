using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSessions.DeleteTemplateSession;

public sealed class DeleteTemplateSessionCommandHandler : ICommandHandler<DeleteTemplateSessionCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public DeleteTemplateSessionCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(DeleteTemplateSessionCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTemplateSessionIdAsync(command.SessionId)
        ?? throw new TemplateSessionNotFoundApplicationException();
        if(command.UserId != program.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TrainingTemplate? trainingTemplate = program.trainingTemplates
            .SingleOrDefault(t => t.TemplateSessions.Any(ts => ts.Id == command.SessionId))
        ?? throw new TemplateSessionNotFoundApplicationException();
        trainingTemplate.DeleteSessionTemplate(command.SessionId, DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
