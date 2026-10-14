using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingSessions.DiscardTrainingSession;

public sealed class DiscardTrainingSessionCommandHandler : ICommandHandler<DiscardTrainingSessionCommand>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public DiscardTrainingSessionCommandHandler(ITrainingSessionRepository trainingSessionRepository, ITrainingUnitOfWork uOW)
    {
        _trainingSessionRepository = trainingSessionRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(DiscardTrainingSessionCommand command, CancellationToken ct = default)
    {
        TrainingSession trainingSession = await _trainingSessionRepository.GetByIdAsync(command.TrainingSessionId)
            ?? throw new TrainingSessionNotFoundApplicationException();
        if(trainingSession.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        trainingSession.EnsureCanBeDiscarded();
        _trainingSessionRepository.Remove(trainingSession);
        await _UOW.SaveChangesAsync(ct);
    }
}
