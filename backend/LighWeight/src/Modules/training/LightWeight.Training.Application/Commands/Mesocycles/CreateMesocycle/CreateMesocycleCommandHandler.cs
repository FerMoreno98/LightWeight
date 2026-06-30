using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

public sealed class CreateMesocycleCommandHandler : ICommandHandler<CreateMesocycleCommand>
{
    public Task HandleAsync(CreateMesocycleCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}