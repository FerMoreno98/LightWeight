using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;

public sealed class CreateTrainingSessionCommandHandler : ICommandHandler<CreateTrainingSessionCommand>
{
    public Task HandleAsync(CreateTrainingSessionCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
