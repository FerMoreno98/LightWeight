using LightWeight.BodyMetrics.Application.Utils;
using LightWeight.BodyMetrics.Domain.Aggregates;
using SkinFoldsAggregate = LightWeight.BodyMetrics.Domain.Aggregates.SkinFolds;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.RegisterSkinFolds;

public sealed class RegisterSkinFoldsCommandHandler : ICommandHandler<RegisterSkinFoldsCommand, Guid>
{
    private readonly ISkinFoldsRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;
    private readonly IClock _clock;

    public RegisterSkinFoldsCommandHandler(ISkinFoldsRepository repository, IBodyMetricsUnitOfWork uOW, IClock clock)
    {
        _repository = repository;
        _UOW = uOW;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(RegisterSkinFoldsCommand command, CancellationToken ct = default)
    {
        // The aggregate validates the values and the date, and raises the event published when saving
        SkinFoldsAggregate measurement = SkinFoldsAggregate.Create
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
