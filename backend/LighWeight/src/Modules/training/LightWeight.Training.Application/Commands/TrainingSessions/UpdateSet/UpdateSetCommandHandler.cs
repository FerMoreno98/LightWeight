using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingSessions.UpdateSet;

public sealed class UpdateSetCommandHandler : ICommandHandler<UpdateSetCommand>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IExerciseRepository _exerciseRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public UpdateSetCommandHandler(ITrainingSessionRepository trainingSessionRepository, IExerciseRepository exerciseRepository, ITrainingUnitOfWork uOW)
    {
        _trainingSessionRepository = trainingSessionRepository;
        _exerciseRepository = exerciseRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(UpdateSetCommand command, CancellationToken ct = default)
    {
        TrainingSession trainingSession = await _trainingSessionRepository.GetByIdAsync(command.TrainingSessionId)
            ?? throw new TrainingSessionNotFoundApplicationException();
        if(trainingSession.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        _ = await _exerciseRepository.GetByIdAsync(command.ExerciseId)
            ?? throw new ExerciseNotFounApplicationException();
        trainingSession.UpdateSet(command.SetId, command.ExerciseId, command.Repetitions, command.Weight, command.RPE, command.IsBodyWeight);
        await _UOW.SaveChangesAsync(ct);
    }
}
