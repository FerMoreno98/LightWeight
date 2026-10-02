using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSets.DeleteTemplateSet;

public sealed class DeleteTemplateSetCommandHandler : ICommandHandler<DeleteTemplateSetCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public DeleteTemplateSetCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork trainingUnitOfWork)
    {
        _UOW = trainingUnitOfWork;
        _programRepository = programRepository;
    }

    public async Task HandleAsync(DeleteTemplateSetCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByTemplateSetIdAsync(command.TemplateSetId)
        ?? throw new TemplateSetNotFoundApplicationException();
        if(command.UserId != program.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TemplateSession? templateSession = program.trainingTemplates
            .SelectMany(t => t.TemplateSessions)
            .SingleOrDefault(s => s.TemplateExercises.Any(ts => ts.Id == command.TemplateSetId))
        ?? throw new TemplateSetNotFoundApplicationException();
        templateSession.DeleteTemplateSet(command.TemplateSetId, DateTime.UtcNow);
        await _UOW.SaveChangesAsync(ct);
        
    }
}