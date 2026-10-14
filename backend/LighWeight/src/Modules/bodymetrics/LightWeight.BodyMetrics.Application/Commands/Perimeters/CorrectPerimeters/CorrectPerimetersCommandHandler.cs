using LightWeight.BodyMetrics.Application.Exceptions;
using LightWeight.BodyMetrics.Application.Utils;
using LightWeight.BodyMetrics.Domain.Aggregates;
using PerimetersAggregate = LightWeight.BodyMetrics.Domain.Aggregates.Perimeters;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.CorrectPerimeters;

public sealed class CorrectPerimetersCommandHandler : ICommandHandler<CorrectPerimetersCommand>
{
    private readonly IPerimetersRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;
    private readonly IClock _clock;

    public CorrectPerimetersCommandHandler(IPerimetersRepository repository, IBodyMetricsUnitOfWork uOW, IClock clock)
    {
        _repository = repository;
        _UOW = uOW;
        _clock = clock;
    }

    public async Task HandleAsync(CorrectPerimetersCommand command, CancellationToken ct = default)
    {
        PerimetersAggregate measurement = await _repository.GetByIdAsync(command.MeasurementId)
            ?? throw new MeasurementNotFoundApplicationException();
        if(measurement.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        measurement.Correct(command.Values.ToMeasures(), UtcDate.Normalize(command.MeasuredAt), _clock.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
