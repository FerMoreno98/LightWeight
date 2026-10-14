using LightWeight.BodyMetrics.Application.Exceptions;
using LightWeight.BodyMetrics.Application.Utils;
using LightWeight.BodyMetrics.Domain.Aggregates;
using SkinFoldsAggregate = LightWeight.BodyMetrics.Domain.Aggregates.SkinFolds;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.CorrectSkinFolds;

public sealed class CorrectSkinFoldsCommandHandler : ICommandHandler<CorrectSkinFoldsCommand>
{
    private readonly ISkinFoldsRepository _repository;
    private readonly IBodyMetricsUnitOfWork _UOW;
    private readonly IClock _clock;

    public CorrectSkinFoldsCommandHandler(ISkinFoldsRepository repository, IBodyMetricsUnitOfWork uOW, IClock clock)
    {
        _repository = repository;
        _UOW = uOW;
        _clock = clock;
    }

    public async Task HandleAsync(CorrectSkinFoldsCommand command, CancellationToken ct = default)
    {
        SkinFoldsAggregate measurement = await _repository.GetByIdAsync(command.MeasurementId)
            ?? throw new MeasurementNotFoundApplicationException();
        if(measurement.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        measurement.Correct(command.Values.ToMeasures(), UtcDate.Normalize(command.MeasuredAt), _clock.UtcNow);
        await _UOW.SaveChangesAsync(ct);
    }
}
