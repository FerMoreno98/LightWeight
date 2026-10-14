using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingSessions.RemoveSet;

public sealed class RemoveSetCommandHandler : ICommandHandler<RemoveSetCommand>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public RemoveSetCommandHandler(ITrainingSessionRepository trainingSessionRepository, ITrainingUnitOfWork uOW)
    {
        _trainingSessionRepository = trainingSessionRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(RemoveSetCommand command, CancellationToken ct = default)
    {
        TrainingSession trainingSession = await _trainingSessionRepository.GetByIdAsync(command.TrainingSessionId)
            ?? throw new TrainingSessionNotFoundApplicationException();
        if(trainingSession.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        trainingSession.RemoveSet(command.SetId);
        await _UOW.SaveChangesAsync(ct);
    }
}
