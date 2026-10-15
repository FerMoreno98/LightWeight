using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetPerimetersEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetSkinFoldsEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetWeightEvolution;
using LightWeight.Dashboard.UnitTests.Fakes;

namespace LightWeight.Dashboard.UnitTests.Application;

public class BodyMetricsEvolutionQueriesTests
{
    // 2026-10-12 is a Monday
    private static readonly DateTime Monday = new(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryBodyMetricsProjectionStore _store = new();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _otherUser = Guid.CreateVersion7();

    private void AddWeight(Guid userId, DateTime at, decimal weight)
        => _store.Weights[Guid.CreateVersion7()] = new WeightProjection { MeasurementId = Guid.CreateVersion7(), UserId = userId, MeasuredAt = at, Weight = weight };

    [Fact]
    public async Task WeightEvolution_ReturnsEveryWeightFromTheOldest_AndTheWeeklyAverages()
    {
        // Arrange
        AddWeight(_userId, Monday.AddDays(8), 80m);
        AddWeight(_userId, Monday, 82m);
        AddWeight(_userId, Monday.AddDays(2), 81m);
        AddWeight(_otherUser, Monday, 70m);
        // Act
        var evolution = await new GetWeightEvolutionQueryHandler(_store).HandleAsync(new GetWeightEvolutionQuery(_userId));
        // Assert
        Assert.Equal([82m, 81m, 80m], evolution.Points.Select(p => p.Weight));
        Assert.Equal([Monday, Monday.AddDays(2), Monday.AddDays(8)], evolution.Points.Select(p => p.MeasuredAt));
        Assert.Equal([81.5m, 80m], evolution.WeeklyAverages.Select(w => w.Average));
        Assert.Equal([2, 1], evolution.WeeklyAverages.Select(w => w.Count));
    }

    [Fact]
    public async Task WeightEvolution_WithoutWeights_ReturnsEmptyLists()
    {
        // Act
        var evolution = await new GetWeightEvolutionQueryHandler(_store).HandleAsync(new GetWeightEvolutionQuery(_userId));
        // Assert
        Assert.Empty(evolution.Points);
        Assert.Empty(evolution.WeeklyAverages);
    }

    [Fact]
    public async Task PerimetersEvolution_ReturnsTheMeasurementsOfTheUserFromTheOldest()
    {
        // Arrange
        _store.Perimeters[Guid.CreateVersion7()] = new PerimetersProjection { UserId = _userId, MeasuredAt = Monday.AddDays(7), Waist = 81m };
        _store.Perimeters[Guid.CreateVersion7()] = new PerimetersProjection { UserId = _userId, MeasuredAt = Monday, Waist = 82m, Neck = 38m };
        _store.Perimeters[Guid.CreateVersion7()] = new PerimetersProjection { UserId = _otherUser, MeasuredAt = Monday, Waist = 70m };
        // Act
        var points = await new GetPerimetersEvolutionQueryHandler(_store).HandleAsync(new GetPerimetersEvolutionQuery(_userId));
        // Assert
        Assert.Equal([82m, 81m], points.Select(p => p.Waist));
        Assert.Equal([38m, null], points.Select(p => p.Neck));
    }

    [Fact]
    public async Task SkinFoldsEvolution_ReturnsTheSkinFoldsAndTheSummatory()
    {
        // Arrange
        _store.SkinFolds[Guid.CreateVersion7()] = new SkinFoldsProjection
        {
            UserId = _userId, MeasuredAt = Monday,
            Abdominal = 15m, Suprailiac = 12m, Tricipital = 9m, Subscapular = 10m, Thigh = 8m, Calf = 6m, Summatory = 60m
        };
        // Act
        var points = await new GetSkinFoldsEvolutionQueryHandler(_store).HandleAsync(new GetSkinFoldsEvolutionQuery(_userId));
        // Assert
        SkinFoldsPoint point = Assert.Single(points);
        Assert.Equal(new SkinFoldsPoint(Monday, 15m, 12m, 9m, 10m, 8m, 6m, 60m), point);
    }

    [Fact]
    public async Task Queries_WithAnEmptyUser_Throw()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new GetWeightEvolutionQueryHandler(_store).HandleAsync(new GetWeightEvolutionQuery(Guid.Empty)));
        await Assert.ThrowsAsync<ArgumentException>(() => new GetPerimetersEvolutionQueryHandler(_store).HandleAsync(new GetPerimetersEvolutionQuery(Guid.Empty)));
        await Assert.ThrowsAsync<ArgumentException>(() => new GetSkinFoldsEvolutionQueryHandler(_store).HandleAsync(new GetSkinFoldsEvolutionQuery(Guid.Empty)));
    }
}
