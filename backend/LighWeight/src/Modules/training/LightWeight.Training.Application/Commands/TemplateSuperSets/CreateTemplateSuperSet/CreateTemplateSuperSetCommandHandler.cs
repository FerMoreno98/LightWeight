using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TemplateSuperSets.CreateTemplateSuperSet;

public sealed class CreateTemplateSuperSetCommandHandler : ICommandHandler<CreateTemplateSuperSetCommand, Guid>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTemplateSuperSetCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateTemplateSuperSetCommand command, CancellationToken ct = default)
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
        // The session checks the sets are its own and cover at least two exercises
        Guid superSetGroupId = templateSession.CreateSuperSet(command.TemplateSetIds);
        await _UOW.SaveChangesAsync(ct);
        return superSetGroupId;
    }
}
