using LightWeight.BodyMetrics.Application.Exceptions;
using LightWeight.BodyMetrics.Domain.Aggregates;
using SkinFoldsAggregate = LightWeight.BodyMetrics.Domain.Aggregates.SkinFolds;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.DeleteSkinFolds;

public sealed class DeleteSkinFoldsCommandHandler : ICommandHandler<DeleteSkinFoldsCommand>
{
    private readonly ISkinFoldsRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;

    public DeleteSkinFoldsCommandHandler(ISkinFoldsRepository repository, IBodyMetricsUnitOfWork uOW)
    {
        _repository = repository;
        _UOW = uOW;
    }

    public async Task HandleAsync(DeleteSkinFoldsCommand command, CancellationToken ct = default)
    {
        SkinFoldsAggregate measurement = await _repository.GetByIdAsync(command.MeasurementId)
            ?? throw new MeasurementNotFoundApplicationException();
        if(measurement.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        // Delete raises the event; Remove deletes the row when saving
        measurement.Delete();
        _repository.Remove(measurement);
        await _UOW.SaveChangesAsync(ct);
    }
}
