using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed class CreateTrainingTemplateCommandHandler : ICommandHandler<CreateTrainingTemplateCommand, Guid>
{
    private readonly ITrainingTemplateRepository _trainingTemplateRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTrainingTemplateCommandHandler(ITrainingTemplateRepository trainingTemplateRepository, ITrainingUnitOfWork uOW)
    {
        _trainingTemplateRepository = trainingTemplateRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateTrainingTemplateCommand command, CancellationToken ct = default)
    {
        var landmark = Enum.Parse<VolumeLandmarks>(command.VolumeLandmark);
        var distribution = Enum.Parse<TrainingDistribution>(command.TrainingDistribution);
        TrainingTemplate template = TrainingTemplate.Create
        (
            command.UserId,
            command.Name,
            landmark,
            distribution
        );
        await _trainingTemplateRepository.AddAsync(template,ct);
        await _UOW.SaveChangesAsync(ct);
        return template.Id;
    }
}
