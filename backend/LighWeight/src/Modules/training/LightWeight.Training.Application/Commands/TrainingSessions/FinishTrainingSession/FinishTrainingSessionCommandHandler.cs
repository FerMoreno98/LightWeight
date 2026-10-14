using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingSessions.FinishTrainingSession;

public sealed class FinishTrainingSessionCommandHandler : ICommandHandler<FinishTrainingSessionCommand, FinishTrainingSessionResponse>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public FinishTrainingSessionCommandHandler
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

    public async Task<FinishTrainingSessionResponse> HandleAsync(FinishTrainingSessionCommand command, CancellationToken ct = default)
    {
        TrainingSession trainingSession = await _trainingSessionRepository.GetByIdAsync(command.TrainingSessionId)
            ?? throw new TrainingSessionNotFoundApplicationException();
        if(trainingSession.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        DateTime now = DateTime.UtcNow;
        trainingSession.Finish(now);

        Macrocycle macrocycle = await _macrocycleRepository.GetByMicrocycleIdAsync(trainingSession.MicrocycleId)
            ?? throw new MicrocycleNotFoundException();
        Microcycle week = macrocycle.Mesocycles
            .SelectMany(m => m.Microcycles)
            .Single(m => m.Id == trainingSession.MicrocycleId);

        // The session and, if it was the last one pending, its week are finished in the same SaveChanges
        bool weekFinished = macrocycle.FinishMicrocycleIfCompleted
        (
            week.Id,
            await GetPlannedSessionIdsAsync(macrocycle, week),
            await GetFinishedSessionIdsAsync(week, trainingSession),
            now
        );
        await _UOW.SaveChangesAsync(ct);
        return new FinishTrainingSessionResponse(weekFinished, week.WeekNumber);
    }

    /// <summary>Sessions of the template the week follows (none if the template has been deleted)</summary>
    private async Task<List<Guid>> GetPlannedSessionIdsAsync(Macrocycle macrocycle, Microcycle week)
    {
        Mesocycle mesocycle = macrocycle.Mesocycles.Single(m => m.Id == week.MesocycleId);
        Program? program = await _programRepository.GetByIdAsync(mesocycle.ProgramId);
        return program?.trainingTemplates
            .SingleOrDefault(t => t.Id == week.TrainingTemplateId)?
            .TemplateSessions.Select(s => s.Id).ToList()
            ?? new();
    }

    /// <summary>Sessions of the template already finished this week, including the one finished now</summary>
    private async Task<List<Guid>> GetFinishedSessionIdsAsync(Microcycle week, TrainingSession finishedNow)
    {
        List<TrainingSession> sessions = await _trainingSessionRepository.GetByMicrocycleIdAsync(week.Id);
        return sessions
            .Where(s => s.IsFinished || s.Id == finishedNow.Id)
            .Select(s => s.TemplateSessionId)
            .Append(finishedNow.TemplateSessionId)
            .OfType<Guid>()
            .Distinct()
            .ToList();
    }
}
