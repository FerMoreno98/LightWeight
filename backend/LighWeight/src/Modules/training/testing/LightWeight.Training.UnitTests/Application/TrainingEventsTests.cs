using LightWeight.shared.Contracts;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Contracts.Training;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using LightWeight.Training.Application.Events;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Events;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class TrainingEventsTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 18, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// A week with a session that covers a planned bench press set (drop set, 6-8 reps, aimed at the chest),
    /// then an extra set of dumbbell press and an extra set of bench press
    /// </summary>
    private sealed class Scenario
    {
        public Guid UserId { get; } = Guid.CreateVersion7();
        public Exercise Bench { get; } = Exercise.Create("Press banca", true, new List<MuscleGroups> { MuscleGroups.Chest, MuscleGroups.Triceps, MuscleGroups.Shoulder });
        public Exercise Dumbbells { get; } = Exercise.Create("Press mancuernas", true, new List<MuscleGroups> { MuscleGroups.Chest });
        public Guid PlannedSetId { get; } = Guid.CreateVersion7();
        public Macrocycle Macrocycle { get; }
        public Mesocycle Mesocycle { get; }
        public Microcycle Week { get; }
        public TrainingSession Session { get; }

        public ITrainingSessionRepository Sessions { get; } = Substitute.For<ITrainingSessionRepository>();
        public IMacrocycleRepository Macrocycles { get; } = Substitute.For<IMacrocycleRepository>();
        public IExerciseRepository Exercises { get; } = Substitute.For<IExerciseRepository>();
        public IProgramRepository Programs { get; } = Substitute.For<IProgramRepository>();
        public IEventPublisher Publisher { get; } = Substitute.For<IEventPublisher>();
        public IClock Clock { get; } = Substitute.For<IClock>();

        public Scenario()
        {
            Macrocycle = Macrocycle.Create(UserId, TrainingStage.Bulk, null, Now.AddDays(-30), null);
            Mesocycle = Macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, Now.AddDays(-30));
            Week = Macrocycle.PlanMicrocycle(Mesocycle.Id, Guid.CreateVersion7(), Now.AddDays(-2));

            Session = TrainingSession.Create(Week.Id, UserId, "Torso A", Now.AddHours(-1), null, 7, 4, 8, Guid.CreateVersion7());
            Session.RegisterSet(Bench.Id, PlannedSetId, AdvanceTrainingTechniques.Create(true, false, false), null, 8, 100m, 8m, false);
            Session.RegisterSet(Dumbbells.Id, null, AdvanceTrainingTechniques.None, null, 12, 30m, 9m, false);
            Session.RegisterSet(Bench.Id, null, AdvanceTrainingTechniques.None, null, 6, 100m, 9.5m, false);
            Session.Finish(Now);

            Sessions.GetByIdAsync(Session.Id).Returns(Session);
            Macrocycles.GetByMicrocycleIdAsync(Week.Id).Returns(Macrocycle);
            Exercises.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Exercise> { Bench, Dumbbells });
            // The planned set was deleted from the template afterwards: only this method still returns it
            Programs.GetPlannedSetsIncludingDeletedAsync(Arg.Any<IReadOnlyCollection<Guid>>())
                .Returns(new List<PlannedSetData> { new(PlannedSetId, 6, 8, new List<MuscleGroups> { MuscleGroups.Chest }) });
            Clock.UtcNow.Returns(Now.AddDays(1));
        }

        public TrainingSessionEventFactory Factory() => new(Exercises, Programs);
        public TrainingSessionDomainEventsHandler DomainHandler() => new(Sessions, Macrocycles, Factory(), Publisher);
        public RebuildRequestedIntegrationEventHandler RebuildHandler() => new(Sessions, Macrocycles, Factory(), Publisher, Clock);

        public TrainingSessionFinishedDomainEvent FinishedEvent()
            => (TrainingSessionFinishedDomainEvent)Session.DomainEvents.Single();

        /// <summary>The integration events received by the publisher</summary>
        public List<TrainingSessionFinishedIntegrationEvent> Published()
            => Publisher.ReceivedCalls()
                .Select(c => c.GetArguments()[0])
                .OfType<TrainingSessionFinishedIntegrationEvent>()
                .ToList();
    }

    // ---------------------------------------------------------------- Domain

    [Fact]
    public void Finish_RaisesTheSessionFinishedEvent()
    {
        // Arrange
        var scenario = new Scenario();
        // Act
        var @event = scenario.FinishedEvent();
        // Assert
        Assert.Equal(scenario.Session.Id, @event.SessionId);
        Assert.Equal(scenario.UserId, @event.UserId);
        Assert.Equal(scenario.Week.Id, @event.MicrocycleId);
        Assert.Equal(Now, @event.FinishedAt);
    }

    [Fact]
    public void SessionInProgress_HasNoEvents()
    {
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Torso A", Now, null, 7, 7, 3);

        Assert.Empty(session.DomainEvents);
    }

    // ---------------------------------------------------------------- Domain event -> integration event

    [Fact]
    public async Task SessionFinished_PublishesTheSessionWithItsCyclesAndWellbeing()
    {
        // Arrange
        var scenario = new Scenario();
        // Act
        await scenario.DomainHandler().HandleAsync(scenario.FinishedEvent());
        // Assert
        var published = Assert.Single(scenario.Published());
        Assert.Equal(EventNames.Training.SessionFinished, published.EventName);
        Assert.Equal(scenario.Session.Id, published.SessionId);
        Assert.Equal(scenario.UserId, published.UserId);
        Assert.Equal(scenario.Macrocycle.Id, published.MacrocycleId);
        Assert.Equal(scenario.Mesocycle.Id, published.MesocycleId);
        Assert.Equal(scenario.Week.Id, published.MicrocycleId);
        Assert.Equal(1, published.WeekNumber);
        Assert.Equal("Torso A", published.Name);
        Assert.Equal(Now.AddHours(-1), published.StartedAt);
        Assert.Equal(Now, published.FinishedAt);
        Assert.Equal((7, 4, 8), (published.Motivation, published.Sleep, published.Doms));
        Assert.Equal(scenario.FinishedEvent().OccurredAtUtc, published.OccurredAtUtc);
    }

    [Fact]
    public async Task SessionFinished_PublishesTheSetsInTheOrderTheyWereRegistered()
    {
        // Arrange
        var scenario = new Scenario();
        // Act
        await scenario.DomainHandler().HandleAsync(scenario.FinishedEvent());
        // Assert
        var sets = Assert.Single(scenario.Published()).Sets;
        Assert.Equal([1, 2, 3], sets.Select(s => s.Order));
        Assert.Equal(scenario.Session.Sets.Select(s => s.Id), sets.Select(s => s.SetId));
        Assert.Equal(["Press banca", "Press mancuernas", "Press banca"], sets.Select(s => s.ExerciseName));
        Assert.Equal([8, 12, 6], sets.Select(s => s.Repetitions));
        Assert.Equal([100m, 30m, 100m], sets.Select(s => s.Weight));
        Assert.Equal([8m, 9m, 9.5m], sets.Select(s => s.Rpe));
    }

    [Fact]
    public async Task PlannedSet_CarriesThePlannedMusclesAndRange_EvenIfItWasDeletedFromTheTemplate()
    {
        // Arrange
        var scenario = new Scenario();
        // Act
        await scenario.DomainHandler().HandleAsync(scenario.FinishedEvent());
        // Assert
        PerformedSetSnapshot planned = Assert.Single(scenario.Published()).Sets[0];
        Assert.True(planned.IsPlanned);
        Assert.Equal("DropSet", planned.Technique);
        Assert.Equal(["Chest"], planned.PlannedMuscleGroups!);
        Assert.Equal(["Chest", "Triceps", "Shoulder"], planned.ExerciseMuscleGroups);
        Assert.Equal((6, 8), (planned.PlannedMinReps, planned.PlannedMaxReps));
    }

    [Fact]
    public async Task ExtraSet_HasNoPlannedData_ButKeepsTheMusclesOfItsExercise()
    {
        // Arrange
        var scenario = new Scenario();
        // Act
        await scenario.DomainHandler().HandleAsync(scenario.FinishedEvent());
        // Assert
        PerformedSetSnapshot extra = Assert.Single(scenario.Published()).Sets[1];
        Assert.False(extra.IsPlanned);
        Assert.Null(extra.Technique);
        Assert.Null(extra.PlannedMuscleGroups);
        Assert.Null(extra.PlannedMinReps);
        Assert.Null(extra.PlannedMaxReps);
        Assert.Equal(["Chest"], extra.ExerciseMuscleGroups);
    }

    // ---------------------------------------------------------------- Rebuild

    [Fact]
    public async Task RebuildRequested_PublishesEveryFinishedSessionOfTheUser()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Sessions.GetFinishedOfAUserAsync(scenario.UserId).Returns(new List<TrainingSession> { scenario.Session });
        scenario.Macrocycles.GetAllOfAUserAsync(scenario.UserId).Returns(new List<Macrocycle> { scenario.Macrocycle });
        // Act
        await scenario.RebuildHandler().HandleAsync(new RebuildRequestedIntegrationEvent(scenario.UserId, Now));
        // Assert
        var published = Assert.Single(scenario.Published());
        Assert.Equal(scenario.Session.Id, published.SessionId);
        Assert.Equal(3, published.Sets.Count);
        Assert.Equal(Now.AddDays(1), published.OccurredAtUtc);
    }

    [Fact]
    public async Task RebuildRequested_OnlyReadsTheSessionsOfTheRequestedUser()
    {
        // Arrange
        var scenario = new Scenario();
        Guid otherUser = Guid.CreateVersion7();
        scenario.Sessions.GetFinishedOfAUserAsync(Arg.Any<Guid>()).Returns(new List<TrainingSession>());
        // Act
        await scenario.RebuildHandler().HandleAsync(new RebuildRequestedIntegrationEvent(scenario.UserId, Now));
        // Assert
        await scenario.Sessions.Received(1).GetFinishedOfAUserAsync(scenario.UserId);
        await scenario.Sessions.DidNotReceive().GetFinishedOfAUserAsync(otherUser);
        Assert.Empty(scenario.Publisher.ReceivedCalls());
    }
}
