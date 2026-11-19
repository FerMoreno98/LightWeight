using LightWeight.Dashboard.Application.Calculations;
using LightWeight.Dashboard.Application.Calculations.Nutrition;
using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Application.Projections.Nutrition;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.Dashboard.Application.Queries.Nutrition.GetNutritionEvolution;

/// <summary>Week by week of the food diary, to read next to the weekly weight</summary>
public sealed record GetNutritionEvolutionQuery(Guid UserId) : IQuery<NutritionEvolutionResponse>;

/// <param name="AverageCalories">Average kcal of every day with diary: the reference of the weeks (null: no diary)</param>
/// <param name="RecentCalories">Average kcal of the days with diary of the last 28 days (null: none)</param>
public sealed record NutritionEvolutionResponse(decimal? AverageCalories, decimal? RecentCalories, List<NutritionWeek> Weeks);

public sealed class GetNutritionEvolutionQueryHandler : IQueryHandler<GetNutritionEvolutionQuery, NutritionEvolutionResponse>
{
    private readonly INutritionProjectionStore _nutrition;
    private readonly IBodyMetricsProjectionStore _bodyMetrics;
    private readonly IClock _clock;

    public GetNutritionEvolutionQueryHandler(INutritionProjectionStore nutrition, IBodyMetricsProjectionStore bodyMetrics, IClock clock)
    {
        _nutrition = nutrition;
        _bodyMetrics = bodyMetrics;
        _clock = clock;
    }

    public async Task<NutritionEvolutionResponse> HandleAsync(GetNutritionEvolutionQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        List<NutritionDayProjection> days = await _nutrition.GetDaysAsync(query.UserId, ct);
        var weights = await _bodyMetrics.GetWeightsAsync(query.UserId, ct);
        List<WeeklyAverage> weeklyWeights = WeeklyAverages.Calculate(weights.Select(w => (w.MeasuredAt, w.Weight)));

        return new NutritionEvolutionResponse(
            NutritionCalculations.AverageCalories(days),
            NutritionCalculations.RecentCalories(days, UserCalendar.Today(_clock.UtcNow)),
            NutritionCalculations.Weeks(days, weeklyWeights));
    }
}
