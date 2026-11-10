using LightWeight.Dashboard.Application.Calculations;
using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Application.Projections.UserProfile;
using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Queries.BodyMetrics.GetSkinFoldsEvolution;

public sealed class GetSkinFoldsEvolutionQueryHandler : IQueryHandler<GetSkinFoldsEvolutionQuery, List<SkinFoldsPoint>>
{
    private readonly IBodyMetricsProjectionStore _store;
    private readonly IUserProfileProjectionStore _profiles;

    public GetSkinFoldsEvolutionQueryHandler(IBodyMetricsProjectionStore store, IUserProfileProjectionStore profiles)
    {
        _store = store;
        _profiles = profiles;
    }

    public async Task<List<SkinFoldsPoint>> HandleAsync(GetSkinFoldsEvolutionQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        var skinFolds = await _store.GetSkinFoldsAsync(query.UserId, ct);
        // Calculated when reading with the current sex: a change of sex recalculates the whole history
        string? sex = await _profiles.GetSexAsync(query.UserId, ct);
        return skinFolds
            .Select(s => new SkinFoldsPoint(s.MeasuredAt, s.Abdominal, s.Suprailiac, s.Tricipital, s.Subscapular, s.Thigh, s.Calf, s.Summatory,
                BodyFat.EstimatePercentage(s.Summatory, sex)))
            .ToList();
    }
}
