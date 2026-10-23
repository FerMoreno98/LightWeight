using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.UnitTests.Fakes;

namespace LightWeight.Dashboard.UnitTests.Application;

public class TrainingCalculationsTests
{
    private static readonly DateTime Monday = TrainingData.Monday;

    // ---------------------------------------------------------------- OneRepMax

    [Fact]
    public void OneRepMax_EpleyWithTheRepetitionsInReserve()
    {
        // 100 kg × 10 @ RPE 8 → RIR 2 → 100 × (1 + 12/30) = 140
        var estimate = OneRepMax.Estimate(100m, 10, 8m, false, null);

        Assert.Equal(new EstimatedOneRepMax(140m, false), estimate);
    }

    [Fact]
    public void OneRepMax_AboveFifteenRepetitionsToFailure_IsFlaggedAsLessPrecise()
    {
        // 60 kg × 15 @ RPE 8 → 17 → 60 × (1 + 17/30) = 94
        var estimate = OneRepMax.Estimate(60m, 15, 8m, false, null);

        Assert.Equal(new EstimatedOneRepMax(94m, true), estimate);
    }

    [Theory]
    [InlineData(40, 25, 8, false)]   // 27 → still estimated
    [InlineData(40, 25, 4, true)]    // 31 → over 30, not estimated
    public void OneRepMax_IsOnlyEstimatedUpToThirtyRepetitionsToFailure(int weight, int reps, int rpe, bool isNull)
    {
        var estimate = OneRepMax.Estimate(weight, reps, rpe, false, null);

        Assert.Equal(isNull, estimate is null);
    }

    [Theory]
    [InlineData(true, null, 100)]       // body weight: the weight is not the real load
    [InlineData(false, "DropSet", 100)] // a technique performs worse by design
    [InlineData(false, "MyoRep", 100)]
    [InlineData(false, null, 0)]        // no load
    public void OneRepMax_IsNotEstimatedForSetsThatCannotBeComparedByTheirLoad(bool bodyWeight, string? technique, int weight)
    {
        Assert.Null(OneRepMax.Estimate(weight, 10, 8m, bodyWeight, technique));
    }

    // ---------------------------------------------------------------- RepZones

    [Theory]
    [InlineData(5, 6, 20, RepZone.Strength)]     // middle 5.5
    [InlineData(6, 6, 20, RepZone.Strength)]     // 6 is the top of strength
    [InlineData(6, 8, 1, RepZone.Hypertrophy)]   // middle 7
    [InlineData(15, 15, 1, RepZone.Hypertrophy)] // 15 is the top of hypertrophy
    [InlineData(15, 16, 1, RepZone.Endurance)]   // middle 15.5
    [InlineData(16, 20, 1, RepZone.Endurance)]
    public void RepZone_ComesFromThePlannedRange_NotFromTheRepetitionsPerformed(int min, int max, int performed, RepZone zone)
    {
        Assert.Equal(zone, RepZones.Of(min, max, performed));
    }

    [Theory]
    [InlineData(6, RepZone.Strength)]
    [InlineData(7, RepZone.Hypertrophy)]
    [InlineData(15, RepZone.Hypertrophy)]
    [InlineData(16, RepZone.Endurance)]
    public void RepZone_OfAnExtraSet_ComesFromTheRepetitionsPerformed(int performed, RepZone zone)
    {
        Assert.Equal(zone, RepZones.Of(null, null, performed));
    }

    // ---------------------------------------------------------------- WeeklyVolume

    [Fact]
    public void WeeklyVolume_PlannedSetsCountTheirPlannedMuscles_ExtraSetsTheMusclesOfTheExercise()
    {
        // Arrange
        var data = new TrainingData();
        Guid week1 = Guid.CreateVersion7();
        var monday = data.Session(Monday, week1, week: 1);
        var thursday = data.Session(Monday.AddDays(3), week1, week: 1);
        var nextWeek = data.Session(Monday.AddDays(7), Guid.CreateVersion7(), week: 2);
        data.Set(monday, data.Bench, 100, 10, 8);                    // planned: Chest
        data.Set(thursday, data.Bench, 100, 10, 8, extra: true);     // extra: Chest + Triceps
        data.Set(nextWeek, data.Squat, 120, 8, 8, plannedMuscles: ["Quads", "Glutes"], exerciseMuscles: ["Quads", "Glutes", "Hamstring"]);
        // Act
        var weeks = WeeklyVolume.Calculate(data.Sessions, data.Sets);
        // Assert
        Assert.Equal(2, weeks.Count);
        Assert.Equal((1, Monday), (weeks[0].WeekNumber, weeks[0].StartedAt));
        Assert.Equal(new Dictionary<string, int> { ["Chest"] = 2, ["Triceps"] = 1 }, weeks[0].SetsByMuscle);
        Assert.Equal(new Dictionary<string, int> { ["Quads"] = 1, ["Glutes"] = 1 }, weeks[1].SetsByMuscle);
    }

    // ---------------------------------------------------------------- SetFatigue

    [Fact]
    public void SetFatigue_FromTheFirstToTheSecondAndToTheLastSet()
    {
        // Arrange: 140 → 130 → 121.7 (100 × (1 + 6.5/30))
        var data = new TrainingData();
        var session = data.Session(Monday);
        data.Set(session, data.Bench, 100, 10, 8);
        data.Set(session, data.Bench, 100, 8, 9);
        data.Set(session, data.Bench, 100, 6, 9.5m);
        // Act
        var fatigue = SetFatigue.Of(ExerciseHistory.Build(data.Sessions, data.Sets).Single());
        // Assert: (140 − 130) / 140 = 7.1 %; (140 − 121.7) / 140 = 13.1 %
        Assert.Equal(new FatiguePoint(Monday, RepZone.Hypertrophy, 7.1m, 13.1m), fatigue);
    }

    [Fact]
    public void SetFatigue_IsNegativeWhenTheSecondSetWasBetter()
    {
        // Arrange: 140 → 143.3 (100 × (1 + 13/30))
        var data = new TrainingData();
        var session = data.Session(Monday);
        data.Set(session, data.Bench, 100, 10, 8);
        data.Set(session, data.Bench, 100, 10, 7);
        // Act
        var fatigue = SetFatigue.Of(ExerciseHistory.Build(data.Sessions, data.Sets).Single());
        // Assert
        Assert.Equal(-2.4m, fatigue!.FirstToSecond);
    }

    [Fact]
    public void SetFatigue_NeedsTwoSetsThatCanBeEstimated()
    {
        // Arrange: the second set is a drop set
        var data = new TrainingData();
        var session = data.Session(Monday);
        data.Set(session, data.Bench, 100, 10, 8);
        data.Set(session, data.Bench, 80, 12, 10, technique: "DropSet");
        // Act & Assert
        Assert.Null(SetFatigue.Of(ExerciseHistory.Build(data.Sessions, data.Sets).Single()));
    }

    // ---------------------------------------------------------------- SessionPerformance

    [Fact]
    public void SessionPerformance_ComparesTheFirstSetWithThePreviousTimeInTheSameZone()
    {
        // Arrange
        var data = new TrainingData();
        var first = data.Session(Monday);
        data.Set(first, data.Bench, 100, 10, 8);                    // 140
        var second = data.Session(Monday.AddDays(7));
        data.Set(second, data.Bench, 100, 10, 7);                   // 143.3 → +2.4 %
        data.Set(second, data.Bench, 100, 6, 10);                   // later sets don't count
        data.Set(second, data.Squat, 120, 8, 8);                    // first time: not compared
        var third = data.Session(Monday.AddDays(14));
        data.Set(third, data.Bench, 120, 5, 8, planned: (3, 5));    // strength: no previous strength session
        // Act
        var points = SessionPerformance.Calculate(data.Sessions, ExerciseHistory.Build(data.Sessions, data.Sets));
        // Assert
        Assert.Equal([null, 2.4m, null], points.Select(p => p.Performance));
        Assert.Equal([0, 1, 0], points.Select(p => p.ComparedExercises));
    }

    [Fact]
    public void SessionPerformance_IsTheAverageOfItsExercises()
    {
        // Arrange
        var data = new TrainingData();
        var first = data.Session(Monday);
        data.Set(first, data.Bench, 100, 10, 8);   // 140
        data.Set(first, data.Squat, 100, 10, 8);   // 140
        var second = data.Session(Monday.AddDays(7));
        data.Set(second, data.Bench, 100, 10, 7);  // 143.3 → +2.4 %
        data.Set(second, data.Squat, 100, 8, 8);   // 133.3 → −4.8 %
        // Act
        var points = SessionPerformance.Calculate(data.Sessions, ExerciseHistory.Build(data.Sessions, data.Sets));
        // Assert: (2.4 − 4.8) / 2 = −1.2
        Assert.Equal(-1.2m, points[1].Performance);
        Assert.Equal(2, points[1].ComparedExercises);
    }

    [Fact]
    public void SessionPerformance_LeavesImplausibleJumpsOutOfTheAverage_AndReportsThem()
    {
        // Arrange: the first bench press was registered with 25 kg (35 of 1RM) instead of 100
        var data = new TrainingData();
        var first = data.Session(Monday);
        data.Set(first, data.Bench, 25, 10, 8);    // 35
        data.Set(first, data.Squat, 100, 10, 8);   // 140
        var second = data.Session(Monday.AddDays(7));
        data.Set(second, data.Bench, 100, 10, 8);  // 140 → +300 %: excluded
        data.Set(second, data.Squat, 100, 8, 8);   // 133.3 → −4.8 %
        var third = data.Session(Monday.AddDays(14));
        data.Set(third, data.Bench, 100, 10, 7);   // 143.3 against 140 → +2.4 %: the reference moved on
        // Act
        var points = SessionPerformance.Calculate(data.Sessions, ExerciseHistory.Build(data.Sessions, data.Sets));
        // Assert
        Assert.Equal((-4.8m, 1), (points[1].Performance, points[1].ComparedExercises));
        Assert.Equal([new ExcludedComparison("Press banca", 300m)], points[1].Excluded);
        Assert.Equal((2.4m, 1), (points[2].Performance, points[2].ComparedExercises));
        Assert.Empty(points[2].Excluded);
    }

    [Theory]
    [InlineData(150, true)]   // +50 %: still plausible
    [InlineData(151, false)]  // +51 %: excluded
    public void SessionPerformance_TheLimitIsFiftyPercent(int secondWeight, bool counted)
    {
        // Arrange: 100 → 150 kg with the same reps and RPE is exactly +50 %
        var data = new TrainingData();
        data.Set(data.Session(Monday), data.Bench, 100, 10, 8);
        data.Set(data.Session(Monday.AddDays(7)), data.Bench, secondWeight, 10, 8);
        // Act
        var second = SessionPerformance.Calculate(data.Sessions, ExerciseHistory.Build(data.Sessions, data.Sets))[1];
        // Assert
        Assert.Equal(counted, second.Performance is not null);
        Assert.Equal(counted ? 0 : 1, second.Excluded.Count);
    }

    // ---------------------------------------------------------------- Trend

    [Fact]
    public void Trend_ComparesTheLastSessionWithThePreviousOneInTheSameZone()
    {
        // Arrange
        var data = new TrainingData();
        data.Set(data.Session(Monday), data.Bench, 100, 10, 8);              // 140
        data.Set(data.Session(Monday.AddDays(7)), data.Bench, 100, 10, 7);  // 143.3
        // Act & Assert
        Assert.Equal(2.4m, ExerciseHistory.Trend(ExerciseHistory.Build(data.Sessions, data.Sets)));
    }

    [Fact]
    public void Trend_WithoutAPreviousSessionInTheSameZone_IsNull()
    {
        // Arrange
        var data = new TrainingData();
        data.Set(data.Session(Monday), data.Bench, 100, 10, 8);
        data.Set(data.Session(Monday.AddDays(7)), data.Bench, 120, 5, 8, planned: (3, 5));
        // Act & Assert
        Assert.Null(ExerciseHistory.Trend(ExerciseHistory.Build(data.Sessions, data.Sets)));
    }
}
