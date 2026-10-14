using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSuperSets.RemoveTemplateSuperSet;

public sealed class RemoveTemplateSuperSetCommandHandler : ICommandHandler<RemoveTemplateSuperSetCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public RemoveTemplateSuperSetCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(RemoveTemplateSuperSetCommand command, CancellationToken ct = default)
    {
        Program program = await _programRepository.GetByTemplateSessionIdAsync(command.TemplateSessionId)
            ?? throw new TemplateSessionNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        TemplateSession templateSession = program.trainingTemplates
            .SelectMany(t => t.TemplateSessions)
            .Single(s => s.Id == command.TemplateSessionId);
        templateSession.RemoveSuperSet(command.SuperSetGroupId);
        await _UOW.SaveChangesAsync(ct);
    }
}
