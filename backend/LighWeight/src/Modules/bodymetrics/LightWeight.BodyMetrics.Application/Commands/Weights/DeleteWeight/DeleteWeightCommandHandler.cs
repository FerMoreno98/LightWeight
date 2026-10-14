using LightWeight.BodyMetrics.Application.Exceptions;
using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.DeleteWeight;

public sealed class DeleteWeightCommandHandler : ICommandHandler<DeleteWeightCommand>
{
    private readonly IWeightMeasurementRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;

    public DeleteWeightCommandHandler(IWeightMeasurementRepository repository, IBodyMetricsUnitOfWork uOW)
    {
        _repository = repository;
        _UOW = uOW;
    }

    public async Task HandleAsync(DeleteWeightCommand command, CancellationToken ct = default)
    {
        WeightMeasurement measurement = await _repository.GetByIdAsync(command.MeasurementId)
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
