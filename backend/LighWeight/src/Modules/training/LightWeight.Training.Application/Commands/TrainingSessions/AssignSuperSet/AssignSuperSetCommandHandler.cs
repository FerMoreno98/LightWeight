using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingSessions.AssignSuperSet;

public sealed class AssignSuperSetCommandHandler : ICommandHandler<AssignSuperSetCommand>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public AssignSuperSetCommandHandler(ITrainingSessionRepository trainingSessionRepository, ITrainingUnitOfWork uOW)
    {
        _trainingSessionRepository = trainingSessionRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(AssignSuperSetCommand command, CancellationToken ct = default)
    {
        TrainingSession trainingSession = await _trainingSessionRepository.GetByIdAsync(command.TrainingSessionId)
            ?? throw new TrainingSessionNotFoundApplicationException();
        if(trainingSession.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        trainingSession.AssignSuperSet(command.SetIds, command.SuperSetGroupId);
        await _UOW.SaveChangesAsync(ct);
    }
}
