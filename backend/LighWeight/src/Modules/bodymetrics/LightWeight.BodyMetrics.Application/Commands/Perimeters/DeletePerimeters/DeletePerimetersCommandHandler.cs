using LightWeight.BodyMetrics.Application.Exceptions;
using LightWeight.BodyMetrics.Domain.Aggregates;
using PerimetersAggregate = LightWeight.BodyMetrics.Domain.Aggregates.Perimeters;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.DeletePerimeters;

public sealed class DeletePerimetersCommandHandler : ICommandHandler<DeletePerimetersCommand>
{
    private readonly IPerimetersRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;

    public DeletePerimetersCommandHandler(IPerimetersRepository repository, IBodyMetricsUnitOfWork uOW)
    {
        _repository = repository;
        _UOW = uOW;
    }

    public async Task HandleAsync(DeletePerimetersCommand command, CancellationToken ct = default)
    {
        PerimetersAggregate measurement = await _repository.GetByIdAsync(command.MeasurementId)
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
