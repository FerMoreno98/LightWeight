using LightWeight.BodyMetrics.Application.Exceptions;
using LightWeight.BodyMetrics.Application.Utils;
using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.CorrectWeight;

public sealed class CorrectWeightCommandHandler : ICommandHandler<CorrectWeightCommand>
{
    private readonly IWeightMeasurementRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;
    private readonly IClock _clock;

    public CorrectWeightCommandHandler(IWeightMeasurementRepository repository, IBodyMetricsUnitOfWork uOW, IClock clock)
    {
        _repository = repository;
        _UOW = uOW;
        _clock = clock;
    }

    public async Task HandleAsync(CorrectWeightCommand command, CancellationToken ct = default)
    {
        WeightMeasurement measurement = await _repository.GetByIdAsync(command.MeasurementId)
            ?? throw new MeasurementNotFoundApplicationException();
        if(measurement.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        measurement.Correct(command.Weight, UtcDate.Normalize(command.MeasuredAt), _clock.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
