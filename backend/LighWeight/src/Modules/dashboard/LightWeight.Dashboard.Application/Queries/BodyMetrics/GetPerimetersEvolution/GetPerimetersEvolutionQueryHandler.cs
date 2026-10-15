using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.BodyMetrics.GetPerimetersEvolution;

public sealed class GetPerimetersEvolutionQueryHandler : IQueryHandler<GetPerimetersEvolutionQuery, List<PerimetersPoint>>
{
    private readonly IBodyMetricsProjectionStore _store;

    public GetPerimetersEvolutionQueryHandler(IBodyMetricsProjectionStore store)
        => _store = store;

    public async Task<List<PerimetersPoint>> HandleAsync(GetPerimetersEvolutionQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        var perimeters = await _store.GetPerimetersAsync(query.UserId, ct);
        return perimeters
            .Select(p => new PerimetersPoint
            (
                p.MeasuredAt,
                p.Neck,
                p.RightArmRelaxed,
                p.RightArmTensioned,
                p.LeftArmRelaxed,
                p.LeftArmTensioned,
                p.Chest,
                p.Shoulder,
                p.Waist,
                p.Hip,
                p.Abs,
                p.RightThigh,
                p.LeftThigh,
                p.RightCalf,
                p.LeftCalf
            ))
            .ToList();
    }
}
