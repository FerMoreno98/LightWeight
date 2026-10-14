using LightWeight.BodyMetrics.Application.Utils;
using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.RegisterWeight;

public sealed class RegisterWeightCommandHandler : ICommandHandler<RegisterWeightCommand, Guid>
{
    private readonly IWeightMeasurementRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;
    private readonly IClock _clock;

    public RegisterWeightCommandHandler(IWeightMeasurementRepository repository, IBodyMetricsUnitOfWork uOW, IClock clock)
    {
        _repository = repository;
        _UOW = uOW;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(RegisterWeightCommand command, CancellationToken ct = default)
    {
        // The aggregate validates the values and the date, and raises the event published when saving
        WeightMeasurement measurement = WeightMeasurement.Create
        (
            command.UserId,
            command.Weight,
            UtcDate.Normalize(command.MeasuredAt),
            _clock.UtcNow
        );
        await _repository.AddAsync(measurement, ct);
        await _UOW.SaveChangesAsync(ct);
        return measurement.Id;
    }
}
