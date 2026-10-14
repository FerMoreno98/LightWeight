using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;

public sealed class CreateTrainingSessionCommandHandler : ICommandHandler<CreateTrainingSessionCommand, Guid>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTrainingSessionCommandHandler
    (
        ITrainingSessionRepository trainingSessionRepository,
        IMacrocycleRepository macrocycleRepository,
        IProgramRepository programRepository,
        ITrainingUnitOfWork uOW
    )
    {
        _trainingSessionRepository = trainingSessionRepository;
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateTrainingSessionCommand command, CancellationToken ct = default)
    {
        // Only the week in progress can be trained
        Macrocycle? macrocycle = await _macrocycleRepository.GetActiveOfAUserAsync(command.UserId);
        Microcycle week = macrocycle?.ActiveMicrocycle
            ?? throw new NoActiveMicrocycleApplicationException();

        Program program = await _programRepository.GetByTemplateSessionIdAsync(command.TemplateSessionId)
            ?? throw new TemplateSessionNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        // The session must be one of the template the week follows
        TemplateSession templateSession = program.trainingTemplates
            .SingleOrDefault(t => t.Id == week.TrainingTemplateId)?
            .TemplateSessions.SingleOrDefault(s => s.Id == command.TemplateSessionId)
            ?? throw new TemplateSessionNotInCurrentWeekApplicationException();

        if(await _trainingSessionRepository.GetByMicrocycleAndTemplateSessionAsync(week.Id, templateSession.Id) is not null)
        {
            throw new TrainingSessionAlreadyExistsApplicationException();
        }

        TrainingSession trainingSession = TrainingSession.Create
        (
            week.Id,
            command.UserId,
            templateSession.Name,
            DateTime.UtcNow,
            command.Comments,
            command.MotivationLevel,
            command.SleepLevel,
            command.DOMSLevel,
            templateSession.Id
        );
        await _trainingSessionRepository.AddAsync(trainingSession, ct);
        await _UOW.SaveChangesAsync(ct);
        return trainingSession.Id;
    }
}
