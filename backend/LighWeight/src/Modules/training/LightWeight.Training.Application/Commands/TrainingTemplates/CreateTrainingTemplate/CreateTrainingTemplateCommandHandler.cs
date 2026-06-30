using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed class CreateTrainingTemplateCommandHandler : ICommandHandler<CreateTrainingTemplateCommand>
{
    public Task HandleAsync(CreateTrainingTemplateCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
