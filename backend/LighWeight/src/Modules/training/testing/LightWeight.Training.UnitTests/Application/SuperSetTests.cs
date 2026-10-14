using LightWeight.Training.Application.Commands.TemplateSets.UpdateTemplateSet;
using LightWeight.Training.Application.Commands.TemplateSuperSets.CreateTemplateSuperSet;
using LightWeight.Training.Application.Commands.TemplateSuperSets.RemoveTemplateSuperSet;
using LightWeight.Training.Application.Commands.TrainingSessions.AssignSuperSet;
using LightWeight.Training.Application.Commands.TrainingSessions.RegisterSet;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Queries.TrainingSessions.GetSessionWorkout;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class SuperSetTests
{
    /// <summary>A program of the user with a session that plans flyes x2 and dips x2</summary>
    private sealed class Scenario
    {
        public Guid UserId { get; } = Guid.CreateVersion7();
        public Program Program { get; }
        public TrainingTemplate Template { get; }
        public TemplateSession Session { get; }
        public List<TemplateSet> Flyes { get; } = new();
        public List<TemplateSet> Dips { get; } = new();
        public Exercise Fly { get; } = Exercise.Create("Aperturas", true, new List<MuscleGroups> { MuscleGroups.Chest });
        public Exercise Dip { get; } = Exercise.Create("Fondos", true, new List<MuscleGroups> { MuscleGroups.Triceps });
        public IProgramRepository Programs { get; } = Substitute.For<IProgramRepository>();
        public IExerciseRepository Exercises { get; } = Substitute.For<IExerciseRepository>();
        public ITrainingSessionRepository Sessions { get; } = Substitute.For<ITrainingSessionRepository>();
        public IMacrocycleRepository Macrocycles { get; } = Substitute.For<IMacrocycleRepository>();
        public ITrainingUnitOfWork UOW { get; } = Substitute.For<ITrainingUnitOfWork>();

        public Scenario()
        {
            Program = Program.Create(UserId, Periodization.Linear, "Hipertrofia", new List<MuscleGroups>());
            Template = TrainingTemplate.Create("Base", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
            Session = TemplateSession.Create("Push");
            for (var i = 0; i < 2; i++)
            {
                Flyes.Add(TemplateSet.Create(Fly.Id, RepetitionRange.Create(12, 10), 8, new List<MuscleGroups> { MuscleGroups.Chest }));
                Dips.Add(TemplateSet.Create(Dip.Id, RepetitionRange.Create(10, 8), 8, new List<MuscleGroups> { MuscleGroups.Triceps }));
            }
            foreach (var set in Flyes.Concat(Dips)) Session.AddSet(set);
            Template.AddSessionTemplate(Session);
            Program.AddTrainingTemplate(Template);
            Programs.GetByTemplateSessionIdAsync(Session.Id).Returns(Program);
            Exercises.GetByIdAsync(Fly.Id).Returns(Fly);
            Exercises.GetByIdAsync(Dip.Id).Returns(Dip);
            Exercises.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Exercise> { Fly, Dip });
        }

        public List<Guid> AllSetIds() => Flyes.Concat(Dips).Select(s => s.Id).ToList();
    }

    // ---------------------------------------------------------------- Templates

    [Fact]
    public async Task CreateTemplateSuperSet_GroupsTheSetsAndReturnsTheId()
    {
        // Arrange
        var scenario = new Scenario();
        var handler = new CreateTemplateSuperSetCommandHandler(scenario.Programs, scenario.UOW);
        // Act
        Guid id = await handler.HandleAsync(new CreateTemplateSuperSetCommand(scenario.Session.Id, scenario.UserId, scenario.AllSetIds()), default);
        // Assert
        Assert.All(scenario.Flyes.Concat(scenario.Dips), s => Assert.Equal(id, s.SuperSetGroupId));
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplateSuperSet_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        var handler = new CreateTemplateSuperSetCommandHandler(scenario.Programs, scenario.UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => handler.HandleAsync(new CreateTemplateSuperSetCommand(scenario.Session.Id, Guid.CreateVersion7(), scenario.AllSetIds()), default)
        );
        Assert.All(scenario.Flyes, s => Assert.Null(s.SuperSetGroupId));
    }

    [Fact]
    public async Task CreateTemplateSuperSet_SessionNotFound_ThrowsApplicationException()
    {
        // Arrange
        var scenario = new Scenario();
        scenario.Programs.GetByTemplateSessionIdAsync(Arg.Any<Guid>()).Returns((Program?)null);
        var handler = new CreateTemplateSuperSetCommandHandler(scenario.Programs, scenario.UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<TemplateSessionNotFoundApplicationException>
        (
            () => handler.HandleAsync(new CreateTemplateSuperSetCommand(Guid.CreateVersion7(), scenario.UserId, scenario.AllSetIds()), default)
        );
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void CreateTemplateSuperSetValidator_NeedsTwoSets(int sets, bool hasErrors)
    {
        var ids = Enumerable.Range(0, sets).Select(_ => Guid.CreateVersion7()).ToList();
        var result = new CreateTemplateSuperSetCommandValidator().Validate(new CreateTemplateSuperSetCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), ids));
        Assert.Equal(hasErrors, !result.IsValid);
    }

    [Fact]
    public async Task RemoveTemplateSuperSet_SeparatesTheSets()
    {
        // Arrange
        var scenario = new Scenario();
        Guid superSetGroupId = scenario.Session.CreateSuperSet(scenario.AllSetIds());
        var handler = new RemoveTemplateSuperSetCommandHandler(scenario.Programs, scenario.UOW);
        // Act
        await handler.HandleAsync(new RemoveTemplateSuperSetCommand(scenario.Session.Id, scenario.UserId, superSetGroupId), default);
        // Assert
        Assert.All(scenario.Flyes.Concat(scenario.Dips), s => Assert.Null(s.SuperSetGroupId));
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveTemplateSuperSet_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        Guid superSetGroupId = scenario.Session.CreateSuperSet(scenario.AllSetIds());
        var handler = new RemoveTemplateSuperSetCommandHandler(scenario.Programs, scenario.UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => handler.HandleAsync(new RemoveTemplateSuperSetCommand(scenario.Session.Id, Guid.CreateVersion7(), superSetGroupId), default)
        );
        Assert.All(scenario.Flyes, s => Assert.Equal(superSetGroupId, s.SuperSetGroupId));
    }

    [Fact]
    public async Task UpdateTemplateSet_KeepsItsSuperSet()
    {
        // Arrange
        var scenario = new Scenario();
        Guid superSetGroupId = scenario.Session.CreateSuperSet(scenario.AllSetIds());
        var handler = new UpdateTemplateSetCommandHandler(scenario.Programs, scenario.UOW);
        // Act
        await handler.HandleAsync(new UpdateTemplateSetCommand
        (
            scenario.Session.Id, scenario.UserId, scenario.Flyes[0].Id, 12, 15, false, false, false, 9, new List<string> { "Chest" }
        ), default);
        // Assert
        Assert.Equal(15, scenario.Flyes[0].RepetitionRange.Max);
        Assert.Equal(superSetGroupId, scenario.Flyes[0].SuperSetGroupId);
    }

    // ---------------------------------------------------------------- Real sets

    [Fact]
    public async Task RegisterSet_StoresTheSuperSetOfTheRequest()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), scenario.UserId, "Push", DateTime.UtcNow, null, 8, 8, 2, scenario.Session.Id);
        scenario.Sessions.GetByIdAsync(session.Id).Returns(session);
        var improvised = Guid.CreateVersion7();
        var handler = new RegisterSetCommandHandler(scenario.Sessions, scenario.Programs, scenario.Exercises, scenario.UOW);
        // Act
        await handler.HandleAsync(new RegisterSetCommand(session.Id, scenario.UserId, scenario.Fly.Id, scenario.Flyes[0].Id, 12, 14m, 8m, false, improvised), default);
        // Assert
        Assert.Equal(improvised, Assert.Single(session.Sets).SuperSetGroupId);
    }

    [Fact]
    public async Task AssignSuperSet_RetagsTheRegisteredSets()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), scenario.UserId, "Push", DateTime.UtcNow, null, 8, 8, 2, scenario.Session.Id);
        Set fly = session.RegisterSet(scenario.Fly.Id, null, AdvanceTrainingTechniques.None, null, 12, 14m, 8m, false);
        Set dip = session.RegisterSet(scenario.Dip.Id, null, AdvanceTrainingTechniques.None, null, 10, 0m, 8m, true);
        scenario.Sessions.GetByIdAsync(session.Id).Returns(session);
        var handler = new AssignSuperSetCommandHandler(scenario.Sessions, scenario.UOW);
        var superSetGroupId = Guid.CreateVersion7();
        // Act
        await handler.HandleAsync(new AssignSuperSetCommand(session.Id, scenario.UserId, new List<Guid> { fly.Id, dip.Id }, superSetGroupId), default);
        // Assert
        Assert.Equal(superSetGroupId, fly.SuperSetGroupId);
        Assert.Equal(superSetGroupId, dip.SuperSetGroupId);
        await scenario.UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignSuperSet_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var scenario = new Scenario();
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), scenario.UserId, "Push", DateTime.UtcNow, null, 8, 8, 2, scenario.Session.Id);
        Set fly = session.RegisterSet(scenario.Fly.Id, null, AdvanceTrainingTechniques.None, null, 12, 14m, 8m, false);
        scenario.Sessions.GetByIdAsync(session.Id).Returns(session);
        var handler = new AssignSuperSetCommandHandler(scenario.Sessions, scenario.UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => handler.HandleAsync(new AssignSuperSetCommand(session.Id, Guid.CreateVersion7(), new List<Guid> { fly.Id }, Guid.CreateVersion7()), default)
        );
        Assert.Null(fly.SuperSetGroupId);
    }

    [Fact]
    public async Task GetSessionWorkout_ReturnsTheSuperSetOfEachSet()
    {
        // Arrange
        var scenario = new Scenario();
        Macrocycle macrocycle = Macrocycle.Create(scenario.UserId, TrainingStage.Bulk, null, DateTime.UtcNow, null);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(scenario.Program.Id, 8, null, null, DateTime.UtcNow);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, scenario.Template.Id, DateTime.UtcNow);
        scenario.Macrocycles.GetActiveOfAUserAsync(scenario.UserId).Returns(macrocycle);
        TrainingSession session = TrainingSession.Create(week.Id, scenario.UserId, "Push", DateTime.UtcNow, null, 8, 8, 2, scenario.Session.Id);
        var superSetGroupId = Guid.CreateVersion7();
        session.RegisterSet(scenario.Fly.Id, scenario.Flyes[0].Id, AdvanceTrainingTechniques.None, superSetGroupId, 12, 14m, 8m, false);
        scenario.Sessions.GetByMicrocycleAndTemplateSessionAsync(week.Id, scenario.Session.Id).Returns(session);
        var handler = new GetSessionWorkoutQueryHandler(scenario.Sessions, scenario.Macrocycles, scenario.Programs, scenario.Exercises);
        // Act
        var result = await handler.HandleAsync(new GetSessionWorkoutQuery(scenario.Session.Id, scenario.UserId), default);
        // Assert
        Assert.NotNull(result.Session);
        Assert.Equal(superSetGroupId, Assert.Single(result.Session.Sets).SuperSetGroupId);
    }
}
