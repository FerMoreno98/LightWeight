using LightWeight.Dashboard.Application.Projectors.Training;
using LightWeight.Dashboard.Application.Queries.Training.GetExerciseProgress;
using LightWeight.Dashboard.Application.Queries.Training.GetExercisesProgress;
using LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;
using LightWeight.Dashboard.Application.Queries.Training.GetTrainingVolume;
using LightWeight.Dashboard.UnitTests.Fakes;
using LightWeight.shared.Contracts.Training;

namespace LightWeight.Dashboard.UnitTests.Application;

public class TrainingProjectionsTests
{
    private static readonly DateTime Monday = TrainingData.Monday;

    // ---------------------------------------------------------------- Projector

    private static TrainingSessionFinishedIntegrationEvent Finished(Guid sessionId, Guid userId, params PerformedSetSnapshot[] sets)
        => new(sessionId, userId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), 2, "Torso A",
            Monday, Monday.AddHours(1), 7, 4, 8, sets, Monday.AddHours(1));

    private static PerformedSetSnapshot Set(Guid setId, int order, int reps, bool planned = true)
        => new(setId, order, Guid.CreateVersion7(), "Press banca", reps, 100m, 8m, false, planned ? "DropSet" : null, planned,
            ["Chest", "Triceps"], planned ? ["Chest"] : null, planned ? 8 : null, planned ? 10 : null);

    [Fact]
    public async Task SessionFinished_StoresTheSessionAndEverySet()
    {
        // Arrange
        var store = new InMemoryTrainingProjectionStore();
        Guid sessionId = Guid.CreateVersion7(), userId = Guid.CreateVersion7(), planned = Guid.CreateVersion7(), extra = Guid.CreateVersion7();
        // Act
        await new TrainingSessionProjector(store).HandleAsync(Finished(sessionId, userId, Set(planned, 1, 10), Set(extra, 2, 8, planned: false)));
        // Assert
        var session = Assert.Single(store.Sessions.Values);
        Assert.Equal((sessionId, userId, 2, "Torso A"), (session.SessionId, session.UserId, session.WeekNumber, session.Name));
        Assert.Equal((7, 4, 8), (session.Motivation, session.Sleep, session.Doms));

        var plannedSet = store.Sets[planned];
        Assert.Equal((sessionId, userId, 1, 10, "DropSet", true), (plannedSet.SessionId, plannedSet.UserId, plannedSet.Order, plannedSet.Repetitions, plannedSet.Technique, plannedSet.IsPlanned));
        Assert.Equal(["Chest"], plannedSet.PlannedMuscleGroups!);
        Assert.Equal(["Chest", "Triceps"], plannedSet.ExerciseMuscleGroups);
        Assert.Equal((8, 10), (plannedSet.PlannedMinReps, plannedSet.PlannedMaxReps));

        var extraSet = store.Sets[extra];
        Assert.Null(extraSet.PlannedMuscleGroups);
        Assert.Null(extraSet.PlannedMinReps);
    }

    [Fact]
    public async Task SessionFinishedTwice_LeavesTheSameSessionAndSets()
    {
        // Arrange: the second time comes from a rebuild
        var store = new InMemoryTrainingProjectionStore();
        var projector = new TrainingSessionProjector(store);
        Guid sessionId = Guid.CreateVersion7(), userId = Guid.CreateVersion7(), set1 = Guid.CreateVersion7(), set2 = Guid.CreateVersion7();
        var @event = Finished(sessionId, userId, Set(set1, 1, 10), Set(set2, 2, 8));
        // Act
        await projector.HandleAsync(@event);
        await projector.HandleAsync(@event);
        // Assert
        Assert.Single(store.Sessions);
        Assert.Equal(2, store.Sets.Count);
    }

    // ---------------------------------------------------------------- Queries

    [Fact]
    public async Task Volume_ReturnsTheWeeksOfTheUser()
    {
        // Arrange
        var data = new TrainingData();
        data.Set(data.Session(Monday), data.Bench, 100, 10, 8);
        var other = new TrainingData();
        other.Set(other.Session(Monday), other.Bench, 100, 10, 8);
        var store = data.Store();
        foreach (var s in other.Sessions) store.Sessions[s.SessionId] = s;
        foreach (var s in other.Sets) store.Sets[s.SetId] = s;
        // Act
        var weeks = await new GetTrainingVolumeQueryHandler(store).HandleAsync(new GetTrainingVolumeQuery(data.UserId));
        // Assert
        var week = Assert.Single(weeks);
        Assert.Equal(1, week.SetsByMuscle["Chest"]);
    }

    [Fact]
    public async Task SessionsPerformance_ReturnsEverySessionWithItsWellbeing()
    {
        // Arrange
        var data = new TrainingData();
        data.Set(data.Session(Monday, sleep: 8), data.Bench, 100, 10, 8);
        data.Set(data.Session(Monday.AddDays(7), sleep: 4, doms: 8, motivation: 5), data.Bench, 100, 8, 8);  // 133.3 → −4.8 %
        // Act
        var sessions = await new GetSessionsPerformanceQueryHandler(data.Store()).HandleAsync(new GetSessionsPerformanceQuery(data.UserId));
        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.Null(sessions[0].Performance);
        Assert.Equal((-4.8m, 1, 5, 4, 8), (sessions[1].Performance, sessions[1].ComparedExercises, sessions[1].Motivation, sessions[1].Sleep, sessions[1].Doms));
    }

    [Fact]
    public async Task ExercisesProgress_FromTheMostRecentlyTrained_WithTheirTrend()
    {
        // Arrange
        var data = new TrainingData();
        data.Set(data.Session(Monday), data.Bench, 100, 10, 8);                  // 140
        data.Set(data.Session(Monday.AddDays(2)), data.Squat, 0, 10, 8, bodyWeight: true);
        data.Set(data.Session(Monday.AddDays(7)), data.Bench, 100, 10, 7);       // 143.3
        // Act
        var exercises = await new GetExercisesProgressQueryHandler(data.Store()).HandleAsync(new GetExercisesProgressQuery(data.UserId));
        // Assert
        Assert.Equal([data.Bench, data.Squat], exercises.Select(e => e.ExerciseId));
        Assert.Equal((2, 143.3m, 2.4m), (exercises[0].Sessions, exercises[0].LastEstimatedOneRepMax, exercises[0].Trend));
        Assert.Equal((1, (decimal?)null, (decimal?)null), (exercises[1].Sessions, exercises[1].LastEstimatedOneRepMax, exercises[1].Trend));
    }

    [Fact]
    public async Task ExerciseProgress_ReturnsProgressionFatigueAndTheRecentSessions()
    {
        // Arrange
        var data = new TrainingData();
        var first = data.Session(Monday, sleep: 8);
        data.Set(first, data.Bench, 100, 10, 8);
        data.Set(first, data.Bench, 100, 8, 9);
        data.Set(data.Session(Monday.AddDays(7), sleep: 4), data.Bench, 105, 10, 8);
        // Act
        var progress = await new GetExerciseProgressQueryHandler(data.Store()).HandleAsync(new GetExerciseProgressQuery(data.UserId, data.Bench));
        // Assert
        Assert.NotNull(progress);
        Assert.Equal("Press banca", progress.Name);
        Assert.Equal([140m, 147m], progress.Progression.Select(p => p.EstimatedOneRepMax));
        Assert.Equal([100m, 105m], progress.Progression.Select(p => p.BestWeight));
        Assert.Equal([8, 4], progress.Progression.Select(p => p.Sleep));
        var fatigue = Assert.Single(progress.Fatigue);
        Assert.Equal(("Hypertrophy", 7.1m), (fatigue.Zone, fatigue.FirstToSecond));
        // The table goes from the most recent session
        Assert.Equal([Monday.AddDays(7), Monday], progress.RecentSessions.Select(s => s.PerformedAt));
        Assert.Equal([1, 2], progress.RecentSessions[1].Sets.Select(s => s.Order));
    }

    [Fact]
    public async Task ExerciseProgress_OfAnExerciseTheUserNeverDid_IsNull()
    {
        // Arrange: another user did it
        var data = new TrainingData();
        data.Set(data.Session(Monday), data.Bench, 100, 10, 8);
        // Act
        var progress = await new GetExerciseProgressQueryHandler(data.Store()).HandleAsync(new GetExerciseProgressQuery(Guid.CreateVersion7(), data.Bench));
        // Assert
        Assert.Null(progress);
    }
}
