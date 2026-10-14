using LightWeight.BodyMetrics.Application.Utils;
using LightWeight.BodyMetrics.Domain.Aggregates;
using PerimetersAggregate = LightWeight.BodyMetrics.Domain.Aggregates.Perimeters;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.RegisterPerimeters;

public sealed class RegisterPerimetersCommandHandler : ICommandHandler<RegisterPerimetersCommand, Guid>
{
    private readonly IPerimetersRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;
    private readonly IClock _clock;

    public RegisterPerimetersCommandHandler(IPerimetersRepository repository, IBodyMetricsUnitOfWork uOW, IClock clock)
    {
        _repository = repository;
        _UOW = uOW;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(RegisterPerimetersCommand command, CancellationToken ct = default)
    {
        // The aggregate validates the values and the date, and raises the event published when saving
        PerimetersAggregate measurement = PerimetersAggregate.Create
        (
            command.UserId,
            command.Values.ToMeasures(),
            UtcDate.Normalize(command.MeasuredAt),
            _clock.UtcNow
        );
        await _repository.AddAsync(measurement, ct);
        await _UOW.SaveChangesAsync(ct);
        return measurement.Id;
    }
}
