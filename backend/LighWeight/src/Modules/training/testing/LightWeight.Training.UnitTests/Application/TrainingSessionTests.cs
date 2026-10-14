using LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.DiscardTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.FinishTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.RegisterSet;
using LightWeight.Training.Application.Commands.TrainingSessions.RemoveSet;
using LightWeight.Training.Application.Commands.TrainingSessions.UpdateSet;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Queries.TrainingSessions.GetPreviousPerformance;
using LightWeight.Training.Application.Queries.TrainingSessions.GetSessionWorkout;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class TrainingSessionTests
{
    /// <summary>A user with a program whose template has one session with a planned drop set, and an active week on that template</summary>
    private sealed class Scenario
    {
        public Guid UserId { get; } = Guid.CreateVersion7();
        public Program Program { get; }
        public TrainingTemplate Template { get; }
        public TemplateSession TemplateSession { get; }
        public TemplateSet PlannedSet { get; }
        public Exercise Bench { get; } = Exercise.Create("Press banca", true, new List<MuscleGroups> { MuscleGroups.Chest });
        public Exercise Dumbbells { get; } = Exercise.Create("Press mancuernas", true, new List<MuscleGroups> { MuscleGroups.Chest });
        public Macrocycle Macrocycle { get; }
        public Microcycle Week { get; }

        public IMacrocycleRepository Macrocycles { get; } = Substitute.For<IMacrocycleRepository>();
        public IProgramRepository Programs { get; } = Substitute.For<IProgramRepository>();
        public IExerciseRepository Exercises { get; } = Substitute.For<IExerciseRepository>();
        public ITrainingSessionRepository Sessions { get; } = Substitute.For<ITrainingSessionRepository>();
        public ITrainingUnitOfWork UOW { get; } = Substitute.For<ITrainingUnitOfWork>();

        public Scenario()
        {
            Program = Program.Create(UserId, Periodization.Linear, "Hipertrofia", new List<MuscleGroups>());
            Template = TrainingTemplate.Create("Base MEV", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
            TemplateSession = TemplateSession.Create("Push");
            PlannedSet = TemplateSet.Create(Bench.Id, RepetitionRange.Create(8, 6), 8, new List<MuscleGroups> { MuscleGroups.Chest },
                AdvanceTrainingTechniques.Create(true, false, false));
            TemplateSession.AddSet(PlannedSet);
            Template.AddSessionTemplate(TemplateSession);
            Program.AddTrainingTemplate(Template);

            Macrocycle = Macrocycle.Create(UserId, TrainingStage.Bulk, null, DateTime.UtcNow, null);
            Mesocycle mesocycle = Macrocycle.PlanMesocycle(Program.Id, 8, null, null, DateTime.UtcNow);
            Week = Macrocycle.PlanMicrocycle(mesocycle.Id, Template.Id, DateTime.UtcNow);

            Macrocycles.GetActiveOfAUserAsync(UserId).Returns(Macrocycle);
            Programs.GetByTemplateSessionIdAsync(TemplateSession.Id).Returns(Program);
            Exercises.GetByIdAsync(Bench.Id).Returns(Bench);
            Exercises.GetByIdAsync(Dumbbells.Id).Returns(Dumbbells);
            Exercises.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Exercise> { Bench, Dumbbells });
            Macrocycles.GetByMicrocycleIdAsync(Week.Id).Returns(Macrocycle);
            Programs.GetByIdAsync(Program.Id).Returns(Program);
            Sessions.GetByMicrocycleIdAsync(Week.Id).Returns(_ => StartedSessions.ToList());
        }

        /// <summary>Sessions returned by GetByMicrocycleIdAsync for the week</summary>
        public List<TrainingSession> StartedSessions { get; } = new();

        public TrainingSession StartSession()
        {
            TrainingSession session = TrainingSession.Create(Week.Id, UserId, "Push", DateTime.UtcNow, null, 8, 8, 2, TemplateSession.Id);
            Sessions.GetByIdAsync(session.Id).Returns(session);
            Sessions.GetByMicrocycleAndTemplateSessionAsync(Week.Id, TemplateSession.Id).Returns(session);
            StartedSessions.Add(session);
            return session;
        }

        public CreateTrainingSessionCommandHandler CreateHandler() => new(Sessions, Macrocycles, Programs, UOW);
        public RegisterSetCommandHandler RegisterHandler() => new(Sessions, Programs, Exercises, UOW);
        public GetSessionWorkoutQueryHandler WorkoutHandler() => new(Sessions, Macrocycles, Programs, Exercises);
        public FinishTrainingSessionCommandHandler FinishHandler() => new(Sessions, Macrocycles, Programs, UOW);
        public DiscardTrainingSessionCommandHandler DiscardHandler() => new(Sessions, UOW);
    }

    // ---------------------------------------------------------------- CreateTrainingSession

    [Fact]
    public async Task CreateTrainingSession_ForASessionOfTheCurrentWeek_StartsItWithTheTemplateSessionName()
    {
        // Arrange
        var scenario = new Scenario();
        // Act
        Guid id = await scenario.CreateHandler().HandleAsync(new CreateTrainingSessionCommand(scenario.UserId, scenario.TemplateSession.Id, 8, 7, 3, "Bien"), default);
        // Assert
        await scenario.Sessions.Received(1).AddAsync
        (
            Arg.Is<TrainingSession>(s =>
                s.Id == id &&
                s.MicrocycleId == scenario.Week.Id &&
                s.TemplateSessionId == scenario.TemplateSession.Id &&
                s.Name == "Push" &&
                s.MotivationLevel == 8 && s.SleepLevel == 7 && s.DOMSLevel == 3 &&
                s.Comments == "Bien"),
            Arg.Any<CancellationToken>()
        );
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTrainingSession_WithoutAWeekInProgress_ThrowsApplicationException()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Macrocycle.FinishMicrocycle(scenario.Week.Id, DateTime.UtcNow);
        // Act
        // Assert
        await Assert.ThrowsAsync<NoActiveMicrocycleApplicationException>
        (
            () => scenario.CreateHandler().HandleAsync(new CreateTrainingSessionCommand(scenario.UserId, scenario.TemplateSession.Id, 8, 7, 3, null), default)
        );
        await scenario.UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTrainingSession_SessionOfAnotherTemplate_ThrowsApplicationException()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingTemplate other = TrainingTemplate.Create("Carga MAV", VolumeLandmarks.MAV, TrainingDistribution.FullBody, 7);
        TemplateSession otherSession = TemplateSession.Create("Pull");
        other.AddSessionTemplate(otherSession);
        scenario.Program.AddTrainingTemplate(other);
        scenario.Programs.GetByTemplateSessionIdAsync(otherSession.Id).Returns(scenario.Program);
        // Act
        // Assert
        await Assert.ThrowsAsync<TemplateSessionNotInCurrentWeekApplicationException>
        (
            () => scenario.CreateHandler().HandleAsync(new CreateTrainingSessionCommand(scenario.UserId, otherSession.Id, 8, 7, 3, null), default)
        );
    }

    [Fact]
    public async Task CreateTrainingSession_AlreadyStartedThisWeek_ThrowsApplicationException()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.StartSession();
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingSessionAlreadyExistsApplicationException>
        (
            () => scenario.CreateHandler().HandleAsync(new CreateTrainingSessionCommand(scenario.UserId, scenario.TemplateSession.Id, 8, 7, 3, null), default)
        );
        await scenario.Sessions.DidNotReceive().AddAsync(Arg.Any<TrainingSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTrainingSession_ProgramOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        var intruder = Guid.CreateVersion7();
        scenario.Macrocycles.GetActiveOfAUserAsync(intruder).Returns(scenario.Macrocycle);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => scenario.CreateHandler().HandleAsync(new CreateTrainingSessionCommand(intruder, scenario.TemplateSession.Id, 8, 7, 3, null), default)
        );
    }

    [Theory]
    [InlineData(0, 5, 5, true)]
    [InlineData(5, 11, 5, true)]
    [InlineData(5, 5, 0, true)]
    [InlineData(1, 10, 5, false)]
    public void CreateTrainingSessionValidator_ChecksTheLevels(int motivation, int sleep, int doms, bool hasErrors)
    {
        var validator = new CreateTrainingSessionCommandValidator();
        var result = validator.Validate(new CreateTrainingSessionCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), motivation, sleep, doms, null));
        Assert.Equal(hasErrors, !result.IsValid);
    }

    // ---------------------------------------------------------------- RegisterSet

    [Fact]
    public async Task RegisterSet_CoveringAPlannedSet_CopiesItsTechnique()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        Guid id = await scenario.RegisterHandler().HandleAsync(new RegisterSetCommand(session.Id, scenario.UserId, scenario.Bench.Id, scenario.PlannedSet.Id, 8, 80m, 8m, false), default);
        // Assert
        Set set = Assert.Single(session.Sets);
        Assert.Equal(id, set.Id);
        Assert.Equal(scenario.PlannedSet.Id, set.TemplateSetId);
        Assert.True(set.AdvanceTrainingTechniques.IsDropSet);
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterSet_WithASubstituteExercise_KeepsThePlannedSet()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        await scenario.RegisterHandler().HandleAsync(new RegisterSetCommand(session.Id, scenario.UserId, scenario.Dumbbells.Id, scenario.PlannedSet.Id, 10, 30m, 8m, false), default);
        // Assert
        Set set = Assert.Single(session.Sets);
        Assert.Equal(scenario.Dumbbells.Id, set.ExerciseId);
        Assert.Equal(scenario.PlannedSet.Id, set.TemplateSetId);
    }

    [Fact]
    public async Task RegisterSet_ExtraSet_HasNoTechniqueNorPlannedSet()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        await scenario.RegisterHandler().HandleAsync(new RegisterSetCommand(session.Id, scenario.UserId, scenario.Dumbbells.Id, null, 12, 20m, 9m, false), default);
        // Assert
        Set set = Assert.Single(session.Sets);
        Assert.Null(set.TemplateSetId);
        Assert.Equal(AdvanceTrainingTechniques.None, set.AdvanceTrainingTechniques);
    }

    [Fact]
    public async Task RegisterSet_PlannedSetOfAnotherSession_ThrowsApplicationException()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        // Assert
        await Assert.ThrowsAsync<TemplateSetNotFoundApplicationException>
        (
            () => scenario.RegisterHandler().HandleAsync(new RegisterSetCommand(session.Id, scenario.UserId, scenario.Bench.Id, Guid.CreateVersion7(), 8, 80m, 8m, false), default)
        );
        Assert.Empty(session.Sets);
    }

    [Fact]
    public async Task RegisterSet_UnknownExercise_ThrowsApplicationException()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        // Assert
        await Assert.ThrowsAsync<ExerciseNotFounApplicationException>
        (
            () => scenario.RegisterHandler().HandleAsync(new RegisterSetCommand(session.Id, scenario.UserId, Guid.CreateVersion7(), null, 8, 80m, 8m, false), default)
        );
    }

    [Fact]
    public async Task RegisterSet_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => scenario.RegisterHandler().HandleAsync(new RegisterSetCommand(session.Id, Guid.CreateVersion7(), scenario.Bench.Id, null, 8, 80m, 8m, false), default)
        );
        Assert.Empty(session.Sets);
    }

    [Fact]
    public async Task RegisterSet_SessionNotFound_ThrowsApplicationException()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Sessions.GetByIdAsync(Arg.Any<Guid>()).Returns((TrainingSession?)null);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingSessionNotFoundApplicationException>
        (
            () => scenario.RegisterHandler().HandleAsync(new RegisterSetCommand(Guid.CreateVersion7(), scenario.UserId, scenario.Bench.Id, null, 8, 80m, 8m, false), default)
        );
    }

    // ---------------------------------------------------------------- UpdateSet / RemoveSet / Finish

    [Fact]
    public async Task UpdateSet_ChangesTheExerciseAndValues()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        Set set = session.RegisterSet(scenario.Bench.Id, scenario.PlannedSet.Id, AdvanceTrainingTechniques.None, null, 8, 80m, 8m, false);
        var handler = new UpdateSetCommandHandler(scenario.Sessions, scenario.Exercises, scenario.UOW);
        // Act
        await handler.HandleAsync(new UpdateSetCommand(session.Id, set.Id, scenario.UserId, scenario.Dumbbells.Id, 10, 32m, 9m, false), default);
        // Assert
        Assert.Equal(scenario.Dumbbells.Id, set.ExerciseId);
        Assert.Equal(10, set.Repetitions);
        Assert.Equal(32m, set.Weight);
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveSet_RemovesItFromTheSession()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        Set set = session.RegisterSet(scenario.Bench.Id, null, AdvanceTrainingTechniques.None, null, 8, 80m, 8m, false);
        var handler = new RemoveSetCommandHandler(scenario.Sessions, scenario.UOW);
        // Act
        await handler.HandleAsync(new RemoveSetCommand(session.Id, set.Id, scenario.UserId), default);
        // Assert
        Assert.Empty(session.Sets);
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveSet_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        Set set = session.RegisterSet(scenario.Bench.Id, null, AdvanceTrainingTechniques.None, null, 8, 80m, 8m, false);
        var handler = new RemoveSetCommandHandler(scenario.Sessions, scenario.UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => handler.HandleAsync(new RemoveSetCommand(session.Id, set.Id, Guid.CreateVersion7()), default)
        );
        Assert.Single(session.Sets);
    }

    [Fact]
    public async Task FinishTrainingSession_LastPendingSessionOfTheWeek_FinishesTheWeekToo()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        var result = await scenario.FinishHandler().HandleAsync(new FinishTrainingSessionCommand(session.Id, scenario.UserId), default);
        // Assert
        Assert.True(session.IsFinished);
        Assert.True(result.WeekFinished);
        Assert.Equal(1, result.WeekNumber);
        Assert.True(scenario.Week.IsFinished);
        Assert.Equal(session.FinishedAt, scenario.Week.FinishedAt);
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishTrainingSession_WithOtherSessionsPending_KeepsTheWeekInProgress()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Template.AddSessionTemplate(TemplateSession.Create("Pull"));
        TrainingSession session = scenario.StartSession();
        // Act
        var result = await scenario.FinishHandler().HandleAsync(new FinishTrainingSessionCommand(session.Id, scenario.UserId), default);
        // Assert
        Assert.True(session.IsFinished);
        Assert.False(result.WeekFinished);
        Assert.False(scenario.Week.IsFinished);
    }

    [Fact]
    public async Task FinishTrainingSession_WhenTheOtherSessionIsOnlyStarted_KeepsTheWeekInProgress()
    {
        // Arrange
        var scenario = new Scenario();
        TemplateSession pull = TemplateSession.Create("Pull");
        scenario.Template.AddSessionTemplate(pull);
        scenario.StartedSessions.Add(TrainingSession.Create(scenario.Week.Id, scenario.UserId, "Pull", DateTime.UtcNow, null, 8, 8, 2, pull.Id));
        TrainingSession session = scenario.StartSession();
        // Act
        var result = await scenario.FinishHandler().HandleAsync(new FinishTrainingSessionCommand(session.Id, scenario.UserId), default);
        // Assert
        Assert.False(result.WeekFinished);
        Assert.False(scenario.Week.IsFinished);
    }

    [Fact]
    public async Task FinishTrainingSession_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => scenario.FinishHandler().HandleAsync(new FinishTrainingSessionCommand(session.Id, Guid.CreateVersion7()), default)
        );
        Assert.False(session.IsFinished);
        Assert.False(scenario.Week.IsFinished);
    }

    // ---------------------------------------------------------------- DiscardTrainingSession

    [Fact]
    public async Task DiscardTrainingSession_InProgress_RemovesItWithItsSets()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        session.RegisterSet(scenario.Bench.Id, scenario.PlannedSet.Id, AdvanceTrainingTechniques.None, null, 8, 80m, 8m, false);
        // Act
        await scenario.DiscardHandler().HandleAsync(new DiscardTrainingSessionCommand(session.Id, scenario.UserId), default);
        // Assert
        scenario.Sessions.Received(1).Remove(session);
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiscardTrainingSession_Finished_ThrowsDomainExceptionAndKeepsIt()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        session.Finish(DateTime.UtcNow);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingSessionFinishedDomainException>
        (
            () => scenario.DiscardHandler().HandleAsync(new DiscardTrainingSessionCommand(session.Id, scenario.UserId), default)
        );
        scenario.Sessions.DidNotReceive().Remove(Arg.Any<TrainingSession>());
    }

    [Fact]
    public async Task DiscardTrainingSession_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => scenario.DiscardHandler().HandleAsync(new DiscardTrainingSessionCommand(session.Id, Guid.CreateVersion7()), default)
        );
        scenario.Sessions.DidNotReceive().Remove(Arg.Any<TrainingSession>());
    }

    // ---------------------------------------------------------------- GetSessionWorkout

    [Fact]
    public async Task GetSessionWorkout_NotStartedYet_CanBeStarted()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Sessions.GetByMicrocycleAndTemplateSessionAsync(scenario.Week.Id, scenario.TemplateSession.Id).Returns((TrainingSession?)null);
        // Act
        var result = await scenario.WorkoutHandler().HandleAsync(new GetSessionWorkoutQuery(scenario.TemplateSession.Id, scenario.UserId), default);
        // Assert
        Assert.True(result.CanStart);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task GetSessionWorkout_Started_ReturnsTheSessionWithItsSets()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = scenario.StartSession();
        session.RegisterSet(scenario.Dumbbells.Id, scenario.PlannedSet.Id, AdvanceTrainingTechniques.None, null, 10, 30m, 8m, false);
        // Act
        var result = await scenario.WorkoutHandler().HandleAsync(new GetSessionWorkoutQuery(scenario.TemplateSession.Id, scenario.UserId), default);
        // Assert
        Assert.False(result.CanStart);
        Assert.NotNull(result.Session);
        Assert.Equal(session.Id, result.Session.Id);
        var set = Assert.Single(result.Session.Sets);
        Assert.Equal("Press mancuernas", set.ExerciseName);
        Assert.Equal(scenario.PlannedSet.Id, set.TemplateSetId);
    }

    [Fact]
    public async Task GetSessionWorkout_WithoutAWeekInProgress_CannotBeStarted()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Macrocycle.FinishMicrocycle(scenario.Week.Id, DateTime.UtcNow);
        // Act
        var result = await scenario.WorkoutHandler().HandleAsync(new GetSessionWorkoutQuery(scenario.TemplateSession.Id, scenario.UserId), default);
        // Assert
        Assert.False(result.CanStart);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task GetSessionWorkout_ProgramOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => scenario.WorkoutHandler().HandleAsync(new GetSessionWorkoutQuery(scenario.TemplateSession.Id, Guid.CreateVersion7()), default)
        );
    }

    // ---------------------------------------------------------------- GetPreviousPerformance

    [Fact]
    public async Task GetPreviousPerformance_SearchesTheWeeksOfTheActiveMacrocycle()
    {
        // Arrange
        var scenario = new Scenario();
        var excluded = Guid.CreateVersion7();
        var performedAt = DateTime.UtcNow.AddDays(-7);
        scenario.Sessions.GetLastSetsOfExercisesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<IReadOnlyCollection<Guid>>(), excluded)
            .Returns(new List<ExercisePerformance>
            {
                new(scenario.Bench.Id, Guid.CreateVersion7(), performedAt, new List<PerformedSet> { new(8, 80m, 8m, false), new(7, 80m, 8.5m, false) })
            });
        var handler = new GetPreviousPerformanceQueryHandler(scenario.Sessions, scenario.Macrocycles);
        // Act
        var result = await handler.HandleAsync(new GetPreviousPerformanceQuery(new[] { scenario.Bench.Id }, excluded, scenario.UserId), default);
        // Assert
        var previous = Assert.Single(result);
        Assert.Equal(scenario.Bench.Id, previous.ExerciseId);
        Assert.Equal(performedAt, previous.PerformedAt);
        Assert.Equal(new[] { 8, 7 }, previous.Sets.Select(s => s.Repetitions));
        await scenario.Sessions.Received(1).GetLastSetsOfExercisesAsync
        (
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == scenario.Week.Id),
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == scenario.Bench.Id),
            excluded
        );
    }

    [Fact]
    public async Task GetPreviousPerformance_WithoutActiveMacrocycle_ReturnsNothing()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Macrocycles.GetActiveOfAUserAsync(scenario.UserId).Returns((Macrocycle?)null);
        var handler = new GetPreviousPerformanceQueryHandler(scenario.Sessions, scenario.Macrocycles);
        // Act
        var result = await handler.HandleAsync(new GetPreviousPerformanceQuery(new[] { scenario.Bench.Id }, null, scenario.UserId), default);
        // Assert
        Assert.Empty(result);
        await scenario.Sessions.DidNotReceive().GetLastSetsOfExercisesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<Guid?>());
    }
}
