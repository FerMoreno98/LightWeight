using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;

public sealed class CreateMicrocycleCommandHandler : ICommandHandler<CreateMicrocycleCommand>
{
    public Task HandleAsync(CreateMicrocycleCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}