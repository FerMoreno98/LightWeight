using LightWeight.Dashboard.Application.Calculations;
using LightWeight.Dashboard.Application.Calculations.Nutrition;
using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Application.Projections.Nutrition;
using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;

public sealed class GetSessionsPerformanceQueryHandler : IQueryHandler<GetSessionsPerformanceQuery, List<SessionPerformanceResponse>>
{
    private readonly ITrainingProjectionStore _store;
    private readonly INutritionProjectionStore _nutrition;
    private readonly IBodyMetricsProjectionStore _bodyMetrics;

    public GetSessionsPerformanceQueryHandler(ITrainingProjectionStore store, INutritionProjectionStore nutrition, IBodyMetricsProjectionStore bodyMetrics)
    {
        _store = store;
        _nutrition = nutrition;
        _bodyMetrics = bodyMetrics;
    }

    public async Task<List<SessionPerformanceResponse>> HandleAsync(GetSessionsPerformanceQuery query, CancellationToken ct = default)
    {
        // The shared mediator does not validate queries
        if (query.UserId == Guid.Empty)
            throw new ArgumentException("The user is required", nameof(query));

        var sessions = await _store.GetSessionsAsync(query.UserId, ct);
        var sets = await _store.GetSetsAsync(query.UserId, ct: ct);
        var history = ExerciseHistory.Build(sessions, sets);
        // Within a macrocycle each session is only compared with the sessions of that macrocycle, like the session screen
        // of Training does: its first time of an exercise has nothing to compare with
        IReadOnlyCollection<TrainingSessionProjection> shown = sessions;
        if (query.MacrocycleId is { } macrocycleId)
        {
            shown = sessions.Where(s => s.MacrocycleId == macrocycleId).ToList();
            history = ExerciseHistory.InMacrocycle(history, macrocycleId);
        }

        // What the user ate around each session: the diary days are calendar days in Europe/Madrid, so is the session's
        Dictionary<DateOnly, NutritionDayProjection> days = (await _nutrition.GetDaysAsync(query.UserId, ct)).ToDictionary(d => d.Date);
        List<WeeklyAverage> weeklyWeights = days.Count == 0
            ? []
            : WeeklyAverages.Calculate((await _bodyMetrics.GetWeightsAsync(query.UserId, ct)).Select(w => (w.MeasuredAt, w.Weight)));
        SessionNutrition? NutritionOf(TrainingSessionProjection session)
        {
            if (days.Count == 0)
                return null;
            DateOnly day = UserCalendar.Today(session.StartedAt);
            return NutritionCalculations.AroundSession(day, days, NutritionCalculations.BodyWeightAt(weeklyWeights, day));
        }

        return SessionPerformance.Calculate(shown, history)
            .Select(p => new SessionPerformanceResponse
            (
                p.Session.SessionId,
                p.Session.Name,
                p.Session.StartedAt,
                p.Performance,
                p.ComparedExercises,
                p.Session.Motivation,
                p.Session.Sleep,
                p.Session.Doms,
                p.Excluded.ToList(),
                NutritionOf(p.Session)
            ))
            .ToList();
    }
}
