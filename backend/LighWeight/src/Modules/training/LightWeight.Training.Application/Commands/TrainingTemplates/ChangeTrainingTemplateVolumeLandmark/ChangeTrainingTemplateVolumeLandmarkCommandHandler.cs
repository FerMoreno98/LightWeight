using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.ChangeTrainingTemplateVolumeLandmark;

public sealed class ChangeTrainingTemplateVolumeLandmarkCommandHandler : ICommandHandler<ChangeTrainingTemplateVolumeLandmarkCommand>
{
    private readonly IProgramRepository _programRepository;
    private readonly ITrainingUnitOfWork _UOW;

    public ChangeTrainingTemplateVolumeLandmarkCommandHandler(IProgramRepository programRepository, ITrainingUnitOfWork uOW)
    {
        _programRepository = programRepository;
        _UOW = uOW;
    }

    public async Task HandleAsync(ChangeTrainingTemplateVolumeLandmarkCommand command, CancellationToken ct = default)
    {
        var volumeLandmark = Enum.Parse<VolumeLandmarks>(command.VolumeLandmark, ignoreCase: true);
        Program? program = await _programRepository.GetByTrainingTemplateIdAsync(command.TrainingTemplateId)
        ?? throw new TrainingTemplateNotFoundApplicationException();
        if(command.UserId != program.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        program.ChangeTrainingTemplateVolumeLandmark(command.TrainingTemplateId, volumeLandmark);
        await _UOW.SaveChangesAsync(ct);
    }
}
