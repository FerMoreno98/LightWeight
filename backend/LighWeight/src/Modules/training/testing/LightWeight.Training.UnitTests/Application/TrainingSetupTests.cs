using LightWeight.Training.Application.Commands.Macrocycles.LaunchTrainingBlock;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Queries.Setup.GetTrainingSetupStatus;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

/// <summary>Backend of the onboarding wizard: the setup status and launching the first block</summary>
public class TrainingSetupTests
{
    private static Program CreateProgram(Guid userId, string name = "PPL Hipertrofia")
    {
        return Program.Create(userId, Periodization.Ondulating, name, new List<MuscleGroups>());
    }

    private static TrainingTemplate AddTemplate(Program program, VolumeLandmarks landmark = VolumeLandmarks.MEV)
    {
        TrainingTemplate template = TrainingTemplate.Create(landmark.ToString(), landmark, TrainingDistribution.PushPullLegs, 7);
        program.AddTrainingTemplate(template);
        return template;
    }

    private static TemplateSession AddSession(TrainingTemplate template, bool withSet)
    {
        TemplateSession session = TemplateSession.Create("Push A");
        if (withSet)
            session.AddSet(TemplateSet.Create(Guid.CreateVersion7(), RepetitionRange.Create(8, 10), 8, new List<MuscleGroups> { MuscleGroups.Chest }));
        template.AddSessionTemplate(session);
        return session;
    }

    private static Macrocycle CreateMacrocycle(Guid userId, bool finished)
    {
        Macrocycle macrocycle = Macrocycle.Create(userId, TrainingStage.Bulk, null, DateTime.UtcNow, null);
        if (finished)
            macrocycle.Finish(DateTime.UtcNow);
        return macrocycle;
    }

    private static GetTrainingSetupStatusQueryHandler StatusHandler
    (
        Guid userId,
        List<Program> programs,
        params Macrocycle[] macrocycles
    )
    {
        IMacrocycleRepository macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        macrocycleRepository.GetAllOfAUserAsync(userId).Returns(macrocycles.ToList());
        IProgramRepository programRepository = Substitute.For<IProgramRepository>();
        programRepository.GetAllProgramsOfAUserAsync(userId).Returns(programs);
        foreach (Program program in programs)
            programRepository.GetByIdAsync(program.Id).Returns(program);
        return new GetTrainingSetupStatusQueryHandler(macrocycleRepository, programRepository);
    }

    // ---------- setup status ----------

    [Fact]
    public async Task GetTrainingSetupStatus_WithoutPrograms_AsksToCreateOne()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var handler = StatusHandler(userId, new List<Program>());
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(TrainingSetupStep.CreateProgram, status.Step);
        Assert.False(status.CanLaunch);
        Assert.True(status.IsFirstTime);
        Assert.Null(status.ProgramId);
        Assert.Empty(status.Templates);
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithProgramWithoutTemplates_AsksToCreateATemplate()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        var handler = StatusHandler(userId, new List<Program> { program });
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(TrainingSetupStep.CreateTemplate, status.Step);
        Assert.Equal(program.Id, status.ProgramId);
        Assert.Equal(program.Name, status.ProgramName);
    }

    [Theory]
    [InlineData(0, 0)]   // template without sessions
    [InlineData(2, 1)]   // one of the two sessions has no sets
    public async Task GetTrainingSetupStatus_WhenTheBaseTemplateIsNotFilled_AsksToFillTheSessions(int sessions, int sessionsWithSets)
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        TrainingTemplate template = AddTemplate(program);
        for (int i = 0; i < sessions; i++)
            AddSession(template, withSet: i < sessionsWithSets);
        var handler = StatusHandler(userId, new List<Program> { program });
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(TrainingSetupStep.FillSessions, status.Step);
        Assert.False(status.CanLaunch);
        SetupTemplateResponse response = Assert.Single(status.Templates);
        Assert.Equal(sessions, response.Sessions);
        Assert.Equal(sessionsWithSets, response.SessionsWithSets);
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithOnlyTheBaseTemplateFilled_OffersVolumeLevelsButCanLaunch()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        AddSession(AddTemplate(program), withSet: true);
        var handler = StatusHandler(userId, new List<Program> { program });
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(TrainingSetupStep.AddVolumeLevels, status.Step);
        Assert.True(status.CanLaunch);
        Assert.Equal("MEV", Assert.Single(status.Templates).VolumeLandmark);
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithSeveralTemplates_AsksToLaunchTheBlock()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        TrainingTemplate baseTemplate = AddTemplate(program, VolumeLandmarks.MEV);
        AddSession(baseTemplate, withSet: true);
        program.DuplicateTrainingTemplate(baseTemplate.Id, "MAV", VolumeLandmarks.MAV);
        var handler = StatusHandler(userId, new List<Program> { program });
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(TrainingSetupStep.LaunchBlock, status.Step);
        Assert.True(status.CanLaunch);
        Assert.Equal(new[] { "MEV", "MAV" }, status.Templates.Select(t => t.VolumeLandmark));
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithAnActiveMacrocycle_IsDone()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        AddSession(AddTemplate(program), withSet: true);
        var handler = StatusHandler(userId, new List<Program> { program }, CreateMacrocycle(userId, finished: false));
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(TrainingSetupStep.Done, status.Step);
        Assert.False(status.CanLaunch);
        Assert.False(status.IsFirstTime);
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithOnlyFinishedMacrocycles_IsNotDoneNorFirstTime()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        AddSession(AddTemplate(program), withSet: true);
        var handler = StatusHandler(userId, new List<Program> { program }, CreateMacrocycle(userId, finished: true));
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(TrainingSetupStep.AddVolumeLevels, status.Step);
        Assert.False(status.IsFirstTime);
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithoutProgramId_UsesTheMostRecentProgram()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program older = CreateProgram(userId, "Viejo");
        // Version 7 ids sort by millisecond: make sure the second one is created later
        await Task.Delay(5);
        Program newer = CreateProgram(userId, "Nuevo");
        var handler = StatusHandler(userId, new List<Program> { newer, older });
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, null));
        // Assert
        Assert.Equal(newer.Id, status.ProgramId);
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithProgramId_UsesThatProgram()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program chosen = CreateProgram(userId, "Elegido");
        await Task.Delay(5);
        Program newer = CreateProgram(userId, "Nuevo");
        AddSession(AddTemplate(newer), withSet: true);
        var handler = StatusHandler(userId, new List<Program> { chosen, newer });
        // Act
        var status = await handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, chosen.Id));
        // Assert
        Assert.Equal(chosen.Id, status.ProgramId);
        Assert.Equal(TrainingSetupStep.CreateTemplate, status.Step);
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithProgramOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program foreign = CreateProgram(Guid.CreateVersion7());
        IMacrocycleRepository macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        macrocycleRepository.GetAllOfAUserAsync(userId).Returns(new List<Macrocycle>());
        IProgramRepository programRepository = Substitute.For<IProgramRepository>();
        programRepository.GetByIdAsync(foreign.Id).Returns(foreign);
        var handler = new GetTrainingSetupStatusQueryHandler(macrocycleRepository, programRepository);
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, foreign.Id)));
    }

    [Fact]
    public async Task GetTrainingSetupStatus_WithUnknownProgramId_ThrowsNotFound()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var handler = StatusHandler(userId, new List<Program>());
        // Act & Assert
        await Assert.ThrowsAsync<ProgramNotFoundApplicationException>(() => handler.HandleAsync(new GetTrainingSetupStatusQuery(userId, Guid.CreateVersion7())));
    }

    // ---------- launch block ----------

    private static (LaunchTrainingBlockCommandHandler handler, IMacrocycleRepository macrocycles, ITrainingUnitOfWork uow) LaunchHandler
    (
        Program? program,
        Macrocycle? activeMacrocycle = null
    )
    {
        IMacrocycleRepository macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork uow = Substitute.For<ITrainingUnitOfWork>();
        if (program is not null)
            programRepository.GetByIdAsync(program.Id).Returns(program);
        macrocycleRepository.GetActiveOfAUserAsync(Arg.Any<Guid>()).Returns(activeMacrocycle);
        return (new LaunchTrainingBlockCommandHandler(macrocycleRepository, programRepository, uow), macrocycleRepository, uow);
    }

    private static LaunchTrainingBlockCommand LaunchCommand(Guid userId, Guid programId, Guid templateId)
    {
        return new LaunchTrainingBlockCommand(userId, "Bulk", null, programId, 8, null, null, templateId);
    }

    [Fact]
    public async Task LaunchTrainingBlockCommand_WhenHappyPath_CreatesMacroMesoAndWeekAndSavesOnce()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        TrainingTemplate mev = AddTemplate(program, VolumeLandmarks.MEV);
        TrainingTemplate mav = AddTemplate(program, VolumeLandmarks.MAV);
        var (handler, macrocycles, uow) = LaunchHandler(program);
        // Act
        var result = await handler.HandleAsync(LaunchCommand(userId, program.Id, mav.Id));
        // Assert
        await macrocycles.Received(1).AddAsync
        (
            Arg.Is<Macrocycle>(m =>
                m.Id == result.MacrocycleId &&
                m.UserId == userId &&
                m.Stage == TrainingStage.Bulk &&
                m.ActiveMesocycle!.Id == result.MesocycleId &&
                m.ActiveMesocycle.ProgramId == program.Id &&
                m.ActiveMesocycle.MotivationLevel == 8 &&
                m.ActiveMicrocycle!.Id == result.MicrocycleId &&
                m.ActiveMicrocycle.TrainingTemplateId == mav.Id &&
                m.ActiveMicrocycle.WeekNumber == 1
            ),
            Arg.Any<CancellationToken>()
        );
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LaunchTrainingBlockCommand_WithAnActiveMacrocycle_ThrowsAndSavesNothing()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        TrainingTemplate template = AddTemplate(program);
        var (handler, macrocycles, uow) = LaunchHandler(program, CreateMacrocycle(userId, finished: false));
        // Act & Assert
        await Assert.ThrowsAsync<ActiveMacrocycleAlreadyExistsDomainException>(() => handler.HandleAsync(LaunchCommand(userId, program.Id, template.Id)));
        await macrocycles.DidNotReceive().AddAsync(Arg.Any<Macrocycle>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LaunchTrainingBlockCommand_WithProgramOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program foreign = CreateProgram(Guid.CreateVersion7());
        TrainingTemplate template = AddTemplate(foreign);
        var (handler, _, uow) = LaunchHandler(foreign);
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(LaunchCommand(userId, foreign.Id, template.Id)));
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LaunchTrainingBlockCommand_WithTemplateOfAnotherProgram_ThrowsNotFound()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        Program program = CreateProgram(userId);
        AddTemplate(program);
        TrainingTemplate otherTemplate = AddTemplate(CreateProgram(userId, "Otro"));
        var (handler, _, uow) = LaunchHandler(program);
        // Act & Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>(() => handler.HandleAsync(LaunchCommand(userId, program.Id, otherTemplate.Id)));
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LaunchTrainingBlockCommand_WithUnknownProgram_ThrowsNotFound()
    {
        // Arrange
        var (handler, _, _) = LaunchHandler(null);
        // Act & Assert
        await Assert.ThrowsAsync<ProgramNotFoundApplicationException>(() =>
            handler.HandleAsync(LaunchCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7())));
    }

    [Theory]
    [InlineData("Bulk", 8, true)]
    [InlineData("cut", 1, true)]
    [InlineData("Maintenance", 10, true)]
    [InlineData("Strength", 8, false)]
    [InlineData("Bulk", 0, false)]
    [InlineData("Bulk", 11, false)]
    public void LaunchTrainingBlockCommandValidator_ValidatesStageAndMotivation(string stage, int motivation, bool isValid)
    {
        // Arrange
        var validator = new LaunchTrainingBlockCommandValidator();
        var command = new LaunchTrainingBlockCommand(Guid.CreateVersion7(), stage, null, Guid.CreateVersion7(), motivation, null, null, Guid.CreateVersion7());
        // Act
        var result = validator.Validate(command);
        // Assert
        Assert.Equal(isValid, result.IsValid);
    }
}
