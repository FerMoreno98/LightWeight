using LightWeight.Dashboard.Application.Calculations;
using LightWeight.Dashboard.Application.Calculations.Nutrition;
using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Application.Projectors.Nutrition;
using LightWeight.Dashboard.Application.Queries.Nutrition.GetNutritionEvolution;
using LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;
using LightWeight.Dashboard.UnitTests.Fakes;
using LightWeight.shared.Contracts.Nutrition;
using LightWeight.shared.Types;
using NSubstitute;

namespace LightWeight.Dashboard.UnitTests.Application;

public class NutritionTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly DateTime Now = new(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryNutritionProjectionStore _nutrition = new();
    private readonly InMemoryBodyMetricsProjectionStore _bodyMetrics = new();

    private void Weight(DateTime at, decimal kg)
        => _bodyMetrics.Weights[Guid.CreateVersion7()] = new WeightProjection { MeasurementId = Guid.CreateVersion7(), UserId = UserId, MeasuredAt = at, Weight = kg };

    // ---------------------------------------------------------------- Projector

    [Fact]
    public async Task Projector_RecordedUpserts_AndDeletedRemovesOnlyThatUsersDay()
    {
        var projector = new NutritionDayProjector(_nutrition);
        Guid dayId = Guid.CreateVersion7();
        var date = new DateOnly(2026, 11, 18);

        await projector.HandleAsync(new DiaryDayRecordedIntegrationEvent(dayId, UserId, date, 2000m, 150m, 250m, 70m, Now));
        await projector.HandleAsync(new DiaryDayRecordedIntegrationEvent(dayId, UserId, date, 2400m, 160m, 280m, 75m, Now));
        Assert.Equal(2400m, Assert.Single(_nutrition.Days.Values).Calories);

        await projector.HandleAsync(new DiaryDayDeletedIntegrationEvent(dayId, Guid.CreateVersion7(), date, Now));
        Assert.Single(_nutrition.Days);
        await projector.HandleAsync(new DiaryDayDeletedIntegrationEvent(dayId, UserId, date, Now));
        Assert.Empty(_nutrition.Days);
        // Deleting a missing day does nothing
        await projector.HandleAsync(new DiaryDayDeletedIntegrationEvent(dayId, UserId, date, Now));
    }

    // ---------------------------------------------------------------- Around a session

    [Fact]
    public void AroundSession_PreviousDayAndSameDay_AndCarbsPerKg()
    {
        var day = new DateOnly(2026, 11, 18);
        var days = new[]
        {
            _nutrition.Add(UserId, day.AddDays(-1), 2000m, carbohydrate: 160m),
            _nutrition.Add(UserId, day, 2600m)
        }.ToDictionary(d => d.Date);

        SessionNutrition? nutrition = NutritionCalculations.AroundSession(day, days, 80m);

        Assert.NotNull(nutrition);
        Assert.Equal(2000m, nutrition.PreviousDayCalories);
        Assert.Equal(2m, nutrition.PreviousDayCarbohydratePerKg);
        Assert.Equal(2600m, nutrition.SameDayCalories);
        // Only one day of the previous 7: not enough for the protein average
        Assert.Null(nutrition.ProteinPerKgLast7Days);
    }

    [Fact]
    public void AroundSession_ProteinOfTheLast7Days_NeedsFourDays()
    {
        var day = new DateOnly(2026, 11, 18);
        var days = new[] { -1, -2, -4, -7 }
            .Select(offset => _nutrition.Add(UserId, day.AddDays(offset), 2400m, protein: 160m))
            .Append(_nutrition.Add(UserId, day.AddDays(-8), 2400m, protein: 400m)) // out of the window
            .ToDictionary(d => d.Date);

        Assert.Equal(2m, NutritionCalculations.AroundSession(day, days, 80m)!.ProteinPerKgLast7Days);

        days.Remove(day.AddDays(-7));
        Assert.Null(NutritionCalculations.AroundSession(day, days, 80m)!.ProteinPerKgLast7Days);
    }

    [Fact]
    public void AroundSession_WithoutWeight_KeepsTheKcalButNoGramsPerKg()
    {
        var day = new DateOnly(2026, 11, 18);
        var days = new[] { _nutrition.Add(UserId, day.AddDays(-1), 2000m) }.ToDictionary(d => d.Date);

        SessionNutrition nutrition = NutritionCalculations.AroundSession(day, days, null)!;

        Assert.Equal(2000m, nutrition.PreviousDayCalories);
        Assert.Null(nutrition.PreviousDayCarbohydratePerKg);
    }

    [Fact]
    public void AroundSession_NothingAround_IsNull()
    {
        var day = new DateOnly(2026, 11, 18);
        var days = new[] { _nutrition.Add(UserId, day.AddDays(-20), 2000m) }.ToDictionary(d => d.Date);

        Assert.Null(NutritionCalculations.AroundSession(day, days, 80m));
    }

    [Fact]
    public void BodyWeightAt_TheWeekOfTheDayOrTheClosestEarlier_AndTheFirstBeforeAnyWeighIn()
    {
        var weeks = new List<WeeklyAverage>
        {
            new(new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc), 81m, 2),
            new(new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc), 80m, 2)
        };

        Assert.Equal(81m, NutritionCalculations.BodyWeightAt(weeks, new DateOnly(2026, 10, 20)));
        Assert.Equal(81m, NutritionCalculations.BodyWeightAt(weeks, new DateOnly(2026, 11, 12)));
        Assert.Equal(80m, NutritionCalculations.BodyWeightAt(weeks, new DateOnly(2026, 11, 16)));
        Assert.Null(NutritionCalculations.BodyWeightAt([], new DateOnly(2026, 11, 16)));
    }

    // ---------------------------------------------------------------- Weeks

    [Fact]
    public void Weeks_MondayToSunday_AverageOfTheDaysWithDiary()
    {
        // Monday 9 and Sunday 15 November are the same week; Monday 16 starts the next one
        _nutrition.Add(UserId, new DateOnly(2026, 11, 9), 2000m, protein: 160m);
        _nutrition.Add(UserId, new DateOnly(2026, 11, 15), 2400m, protein: 160m);
        _nutrition.Add(UserId, new DateOnly(2026, 11, 16), 3000m);
        var weights = new List<WeeklyAverage> { new(new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc), 80m, 1) };

        List<NutritionWeek> weeks = NutritionCalculations.Weeks(_nutrition.Days.Values, weights);

        Assert.Equal(2, weeks.Count);
        Assert.Equal(new NutritionWeek(new DateOnly(2026, 11, 9), 2200m, 2, 2m), weeks[0]);
        Assert.Equal((new DateOnly(2026, 11, 16), 3000m, 1), (weeks[1].WeekStart, weeks[1].Calories, weeks[1].DaysLogged));
    }

    [Fact]
    public async Task Evolution_AverageOfEveryDay_AndOfTheLast28Days()
    {
        _nutrition.Add(UserId, new DateOnly(2026, 9, 1), 3000m);
        _nutrition.Add(UserId, new DateOnly(2026, 11, 1), 2000m);
        _nutrition.Add(UserId, new DateOnly(2026, 11, 18), 2200m);
        _nutrition.Add(Guid.CreateVersion7(), new DateOnly(2026, 11, 18), 5000m);
        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        NutritionEvolutionResponse evolution = await new GetNutritionEvolutionQueryHandler(_nutrition, _bodyMetrics, clock)
            .HandleAsync(new GetNutritionEvolutionQuery(UserId));

        Assert.Equal(2400m, evolution.AverageCalories);
        Assert.Equal(2100m, evolution.RecentCalories);
        Assert.Equal(3, evolution.Weeks.Count);
        Assert.All(evolution.Weeks, w => Assert.Null(w.ProteinPerKg));
    }

    // ---------------------------------------------------------------- Sessions

    [Fact]
    public async Task SessionsPerformance_CarryWhatWasEatenAroundEachSession_InMadrid()
    {
        var data = new TrainingData();
        // 23:30 UTC on Monday 16 November is already Tuesday 17 in Madrid: the previous day is Monday
        data.Set(data.Session(new DateTime(2026, 11, 16, 23, 30, 0, DateTimeKind.Utc)), data.Bench, 100, 10, 8);
        _nutrition.Add(data.UserId, new DateOnly(2026, 11, 16), 1900m, carbohydrate: 152m);
        _nutrition.Add(data.UserId, new DateOnly(2026, 11, 17), 2500m);
        _bodyMetrics.Weights[Guid.CreateVersion7()] = new WeightProjection
            { MeasurementId = Guid.CreateVersion7(), UserId = data.UserId, MeasuredAt = new DateTime(2026, 11, 10, 7, 0, 0, DateTimeKind.Utc), Weight = 76m };

        var sessions = await new GetSessionsPerformanceQueryHandler(data.Store(), _nutrition, _bodyMetrics)
            .HandleAsync(new GetSessionsPerformanceQuery(data.UserId));

        SessionNutrition nutrition = Assert.Single(sessions).Nutrition!;
        Assert.Equal(1900m, nutrition.PreviousDayCalories);
        Assert.Equal(2m, nutrition.PreviousDayCarbohydratePerKg);
        Assert.Equal(2500m, nutrition.SameDayCalories);
    }

    [Fact]
    public async Task SessionsPerformance_WithoutDiary_HaveNoNutrition()
    {
        var data = new TrainingData();
        data.Set(data.Session(TrainingData.Monday), data.Bench, 100, 10, 8);

        var sessions = await new GetSessionsPerformanceQueryHandler(data.Store(), _nutrition, _bodyMetrics)
            .HandleAsync(new GetSessionsPerformanceQuery(data.UserId));

        Assert.Null(Assert.Single(sessions).Nutrition);
    }
}
