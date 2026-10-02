using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed class CreateTrainingTemplateCommandHandler : ICommandHandler<CreateTrainingTemplateCommand, Guid>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public CreateTrainingTemplateCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task<Guid> HandleAsync(CreateTrainingTemplateCommand command, CancellationToken ct = default)
    {
        Program? program = await _programRepository.GetByIdAsync(command.ProgramId)
        ?? throw new ProgramNotFoundApplicationException();
        if(program.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        var landmark = Enum.Parse<VolumeLandmarks>(command.VolumeLandmark);
        var distribution = Enum.Parse<TrainingDistribution>(command.TrainingDistribution);
        TrainingTemplate template = TrainingTemplate.Create
        (
            landmark,
            distribution,
            command.DurationInDays,
            command.Order
        );
        program.AddTrainingTemplate(template);
        await _UOW.SaveChangesAsync(ct);
        return template.Id;
    }
}

