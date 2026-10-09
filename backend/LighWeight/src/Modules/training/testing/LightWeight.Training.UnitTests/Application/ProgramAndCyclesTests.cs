using LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;
using LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;
using LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;
using LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;
using LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;
using LightWeight.Training.Application.Queries.Macrocycles.GetMacrocycleDetail;
using LightWeight.Training.Application.Queries.Macrocycles.GetUserMacrocycles;
using LightWeight.Training.Application.Queries.Mesocycles.GetMesocycleDetail;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Application.Commands.Programs.CreateProgram;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Queries.Programs.GetUserPrograms;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class ProgramAndCyclesTests
{
    private static Program CreateProgram(Guid userId)
    {
        return Program.Create
        (
            userId,
            Periodization.Linear,
            "ValidProgramName",
            new List<MuscleGroups>()
        );
    }

    private static Macrocycle CreateActiveMacrocycle(Guid userId)
    {
        return Macrocycle.Create(userId, TrainingStage.Bulk, null, DateTime.UtcNow, null);
    }

    private static Mesocycle CreateActiveMesocycle(Guid userId, Guid programId)
    {
        return CreateActiveMacrocycle(userId).PlanMesocycle(new List<Mesocycle>(), programId, 8, null, null, DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateProgramCommand_WithValidData_AddsTheProgram()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        CreateProgramCommandHandler commandHandler = new CreateProgramCommandHandler(_programRepository,_UOW);
        CreateProgramCommand command = new CreateProgramCommand
        (
            userId,
            "ValidName",
            "Ondulating",
            new List<string> { "Back", "Chest" }
        );
        // Act
        Guid programId = await commandHandler.HandleAsync(command,default);
        // Assert
        Assert.NotEqual(Guid.Empty, programId);
        await _programRepository.Received(1).AddAsync
        (
            Arg.Is<Program>(p =>
                p.Id == programId &&
                p.UserId == userId &&
                p.Periodization == Periodization.Ondulating &&
                p.AimMuscleGroups.SequenceEqual(new[] { MuscleGroups.Back, MuscleGroups.Chest })),
            Arg.Any<CancellationToken>()
        );
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Macrocycles

    [Fact]
    public async Task CreateMacrocycleCommand_WithNoActiveMacrocycle_AddsItAndReturnsTheId()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        _macrocycleRepository.GetActiveOfAUserAsync(userId).Returns((Macrocycle?)null);
        CreateMacrocycleCommandHandler commandHandler = new CreateMacrocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        Guid id = await commandHandler.HandleAsync(new CreateMacrocycleCommand(userId,"Bulk","Volumen 2026"),default);
        // Assert
        await _macrocycleRepository.Received(1).AddAsync
        (
            Arg.Is<Macrocycle>(m => m.Id == id && m.UserId == userId && m.Stage == TrainingStage.Bulk && !m.IsFinished),
            Arg.Any<CancellationToken>()
        );
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMacrocycleCommand_WithAnActiveMacrocycle_ThrowsDomainExceptionAndDoesNotSave()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        _macrocycleRepository.GetActiveOfAUserAsync(userId).Returns(CreateActiveMacrocycle(userId));
        CreateMacrocycleCommandHandler commandHandler = new CreateMacrocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<ActiveMacrocycleAlreadyExistsDomainException>
        (
            () => commandHandler.HandleAsync(new CreateMacrocycleCommand(userId,"Cut",null),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMacrocycleCommand_FinishesTheMacrocycleAndItsActiveMesocycle()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(new List<Mesocycle>(), Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _mesocycleRepository.GetByMacrocycleIdAsync(macrocycle.Id).Returns(new List<Mesocycle> { mesocycle });
        FinishMacrocycleCommandHandler commandHandler =
            new FinishMacrocycleCommandHandler(_macrocycleRepository,_mesocycleRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new FinishMacrocycleCommand(macrocycle.Id,userId),default);
        // Assert
        Assert.True(macrocycle.IsFinished);
        Assert.True(mesocycle.IsFinished);
        Assert.Equal(macrocycle.FinishedAt, mesocycle.FinishedAt);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMacrocycleCommand_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        FinishMacrocycleCommandHandler commandHandler =
            new FinishMacrocycleCommandHandler(_macrocycleRepository,_mesocycleRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new FinishMacrocycleCommand(macrocycle.Id,Guid.CreateVersion7()),default)
        );
        Assert.False(macrocycle.IsFinished);
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMacrocycleCommand_NotFound_ThrowsApplicationException()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        _macrocycleRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Macrocycle?)null);
        FinishMacrocycleCommandHandler commandHandler =
            new FinishMacrocycleCommandHandler(_macrocycleRepository,_mesocycleRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<MacrocycleNotFoundException>
        (
            () => commandHandler.HandleAsync(new FinishMacrocycleCommand(Guid.CreateVersion7(),Guid.CreateVersion7()),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Mesocycles

    [Fact]
    public async Task CreateMesocycleCommand_WithValidData_AddsTheMesocycleLinkedToTheProgram()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Program program = CreateProgram(userId);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        _mesocycleRepository.GetByMacrocycleIdAsync(macrocycle.Id).Returns(new List<Mesocycle>());
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        Guid id = await commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,program.Id,8,"Hombro",null),default);
        // Assert
        await _mesocycleRepository.Received(1).AddAsync
        (
            Arg.Is<Mesocycle>(m => m.Id == id && m.ProgramId == program.Id && m.MacrocycleId == macrocycle.Id && !m.IsFinished),
            Arg.Any<CancellationToken>()
        );
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMesocycleCommand_WithAnActiveMesocycle_ThrowsDomainExceptionAndDoesNotSave()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Program program = CreateProgram(userId);
        Mesocycle active = macrocycle.PlanMesocycle(new List<Mesocycle>(), program.Id, 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        _mesocycleRepository.GetByMacrocycleIdAsync(macrocycle.Id).Returns(new List<Mesocycle> { active });
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<ActiveMesocycleAlreadyExistsDomainException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,program.Id,8,null,null),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMesocycleCommand_MacrocycleOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,Guid.CreateVersion7(),8,null,null),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMesocycleCommand_ProgramOfAnotherUser_ThrowApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Program program = CreateProgram(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,program.Id,8,null,null),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMesocycleCommand_ProgramNotFound_ThrowApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var fakeProgramId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(fakeProgramId).Returns((Program?) null);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<ProgramNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,fakeProgramId,8,null,null),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMesocycleCommand_FinishesTheMesocycleAndSaves()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Mesocycle mesocycle = CreateActiveMesocycle(userId, Guid.CreateVersion7());
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        FinishMesocycleCommandHandler commandHandler = new FinishMesocycleCommandHandler(_mesocycleRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new FinishMesocycleCommand(mesocycle.Id,userId),default);
        // Assert
        Assert.True(mesocycle.IsFinished);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMesocycleCommand_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Mesocycle mesocycle = CreateActiveMesocycle(Guid.CreateVersion7(), Guid.CreateVersion7());
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        FinishMesocycleCommandHandler commandHandler = new FinishMesocycleCommandHandler(_mesocycleRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new FinishMesocycleCommand(mesocycle.Id,Guid.CreateVersion7()),default)
        );
        Assert.False(mesocycle.IsFinished);
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Microcycles

    [Fact]
    public async Task CreateMicrocycleCommand_WithTemplateOfTheMesocycleProgram_AddsTheFirstWeek()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(template);
        Mesocycle mesocycle = CreateActiveMesocycle(userId, program.Id);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        _microcycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(new List<Microcycle>());
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        Guid id = await commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default);
        // Assert
        await _microcycleRepository.Received(1).AddAsync
        (
            Arg.Is<Microcycle>(m => m.Id == id && m.TrainingTemplateId == template.Id && m.WeekNumber == 1),
            Arg.Any<CancellationToken>()
        );
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMicrocycleCommand_WithPreviousWeeks_NumbersTheNextWeek()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(template);
        Mesocycle mesocycle = CreateActiveMesocycle(userId, program.Id);
        Microcycle week1 = mesocycle.PlanMicrocycle(new List<Microcycle>(), template.Id);
        Microcycle week2 = mesocycle.PlanMicrocycle(new List<Microcycle> { week1 }, template.Id);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        _microcycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(new List<Microcycle> { week1, week2 });
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default);
        // Assert
        await _microcycleRepository.Received(1).AddAsync
        (
            Arg.Is<Microcycle>(m => m.WeekNumber == 3),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateMicrocycleCommand_InAFinishedMesocycle_ThrowsDomainExceptionAndDoesNotSave()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(template);
        Mesocycle mesocycle = CreateActiveMesocycle(userId, program.Id);
        mesocycle.Finish(DateTime.UtcNow);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        _microcycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(new List<Microcycle>());
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<MesocycleFinishedDomainException>
        (
            () => commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMicrocycleCommand_WithTemplateOutsideTheMesocycleProgram_ThrowApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        Mesocycle mesocycle = CreateActiveMesocycle(userId, program.Id);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,Guid.CreateVersion7()),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMicrocycleCommand_WithDeletedTemplate_ThrowApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(template);
        program.DeleteTrainingTemplate(template.Id, DateTime.UtcNow);
        Mesocycle mesocycle = CreateActiveMesocycle(userId, program.Id);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Cycle queries

    [Fact]
    public async Task GetUserMacrocyclesQuery_ReturnsTheActiveFirstThenTheFinishedFromTheMostRecent()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        Macrocycle oldest = Macrocycle.Create(userId, TrainingStage.Bulk, null, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), null);
        oldest.Finish(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), new List<Mesocycle>());
        Macrocycle older = Macrocycle.Create(userId, TrainingStage.Cut, null, new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc), null);
        Mesocycle olderMesocycle1 = older.PlanMesocycle(new List<Mesocycle>(), Guid.CreateVersion7(), 8, null, null, new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        olderMesocycle1.Finish(new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        Mesocycle olderMesocycle2 = older.PlanMesocycle(new List<Mesocycle> { olderMesocycle1 }, Guid.CreateVersion7(), 8, null, null, new DateTime(2025, 7, 2, 0, 0, 0, DateTimeKind.Utc));
        older.Finish(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), new List<Mesocycle> { olderMesocycle1, olderMesocycle2 });
        Macrocycle active = Macrocycle.Create(userId, TrainingStage.Maintenance, "Actual", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null);
        _macrocycleRepository.GetAllOfAUserAsync(userId).Returns(new List<Macrocycle> { oldest, active, older });
        _mesocycleRepository.GetAllOfAUserAsync(userId).Returns(new List<Mesocycle> { olderMesocycle1, olderMesocycle2 });
        GetUserMacrocyclesQueryHandler queryHandler = new GetUserMacrocyclesQueryHandler(_macrocycleRepository,_mesocycleRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetUserMacrocyclesQuery(userId),default);
        // Assert
        Assert.Equal(new[] { active.Id, older.Id, oldest.Id }, result.Select(r => r.Id));
        Assert.Null(result[0].FinishedAt);
        Assert.Equal("Maintenance", result[0].Stage);
        Assert.Equal(0, result[0].MesocyclesCount);
        Assert.Equal(2, result[1].MesocyclesCount);
    }

    [Fact]
    public async Task GetMacrocycleDetailQuery_ReturnsTheMesocyclesInOrderWithTheirProgramAndWeeks()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(template);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle first = macrocycle.PlanMesocycle(new List<Mesocycle>(), program.Id, 7, "Rodilla", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Microcycle week1 = first.PlanMicrocycle(new List<Microcycle>(), template.Id);
        first.Finish(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        Mesocycle second = macrocycle.PlanMesocycle(new List<Mesocycle> { first }, program.Id, 9, null, null, new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc));
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _mesocycleRepository.GetByMacrocycleIdAsync(macrocycle.Id).Returns(new List<Mesocycle> { second, first });
        _microcycleRepository.GetByMesocycleIdAsync(first.Id).Returns(new List<Microcycle> { week1 });
        _microcycleRepository.GetByMesocycleIdAsync(second.Id).Returns(new List<Microcycle>());
        _programRepository.GetAllProgramsOfAUserAsync(userId).Returns(new List<Program> { program });
        GetMacrocycleDetailQueryHandler queryHandler =
            new GetMacrocycleDetailQueryHandler(_macrocycleRepository,_mesocycleRepository,_microcycleRepository,_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetMacrocycleDetailQuery(macrocycle.Id,userId),default);
        // Assert
        Assert.Equal(macrocycle.Id, result.Id);
        Assert.Null(result.FinishedAt);
        Assert.Equal(new[] { first.Id, second.Id }, result.Mesocycles.Select(m => m.Id));
        Assert.Equal("ValidProgramName", result.Mesocycles[0].ProgramName);
        Assert.Equal("Rodilla", result.Mesocycles[0].Injuries);
        Assert.Equal(1, result.Mesocycles[0].MicrocyclesCount);
        Assert.NotNull(result.Mesocycles[0].FinishedAt);
        Assert.Null(result.Mesocycles[1].FinishedAt);
    }

    [Fact]
    public async Task GetMacrocycleDetailQuery_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        GetMacrocycleDetailQueryHandler queryHandler = new GetMacrocycleDetailQueryHandler
        (
            _macrocycleRepository,
            Substitute.For<IMesocycleRepository>(),
            Substitute.For<IMicrocycleRepository>(),
            Substitute.For<IProgramRepository>()
        );
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => queryHandler.HandleAsync(new GetMacrocycleDetailQuery(macrocycle.Id,Guid.CreateVersion7()),default)
        );
    }

    [Fact]
    public async Task GetMacrocycleDetailQuery_NotFound_ThrowsApplicationException()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        _macrocycleRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Macrocycle?)null);
        GetMacrocycleDetailQueryHandler queryHandler = new GetMacrocycleDetailQueryHandler
        (
            _macrocycleRepository,
            Substitute.For<IMesocycleRepository>(),
            Substitute.For<IMicrocycleRepository>(),
            Substitute.For<IProgramRepository>()
        );
        // Act
        // Assert
        await Assert.ThrowsAsync<MacrocycleNotFoundException>
        (
            () => queryHandler.HandleAsync(new GetMacrocycleDetailQuery(Guid.CreateVersion7(),Guid.CreateVersion7()),default)
        );
    }

    [Fact]
    public async Task GetMesocycleDetailQuery_ReturnsTheWeeksInOrderAndTheTemplatesOfTheProgram()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        Program program = CreateProgram(userId);
        TrainingTemplate baseTemplate = TrainingTemplate.Create("Base",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        TrainingTemplate deload = TrainingTemplate.Create("Deload",VolumeLandmarks.MV,TrainingDistribution.FullBody,5);
        TrainingTemplate removed = TrainingTemplate.Create("Removed",VolumeLandmarks.MAV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(baseTemplate);
        program.AddTrainingTemplate(deload);
        program.AddTrainingTemplate(removed);
        Mesocycle mesocycle = CreateActiveMesocycle(userId, program.Id);
        Microcycle week1 = mesocycle.PlanMicrocycle(new List<Microcycle>(), baseTemplate.Id);
        Microcycle week2 = mesocycle.PlanMicrocycle(new List<Microcycle> { week1 }, removed.Id);
        Microcycle week3 = mesocycle.PlanMicrocycle(new List<Microcycle> { week1, week2 }, deload.Id);
        program.DeleteTrainingTemplate(removed.Id, DateTime.UtcNow);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        _microcycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(new List<Microcycle> { week3, week1, week2 });
        GetMesocycleDetailQueryHandler queryHandler =
            new GetMesocycleDetailQueryHandler(_mesocycleRepository,_microcycleRepository,_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetMesocycleDetailQuery(mesocycle.Id,userId),default);
        // Assert
        Assert.Equal("ValidProgramName", result.ProgramName);
        Assert.Equal(mesocycle.MacrocycleId, result.MacrocycleId);
        Assert.Equal(new[] { 1, 2, 3 }, result.Microcycles.Select(m => m.WeekNumber));
        Assert.Equal("Base", result.Microcycles[0].TemplateName);
        Assert.Equal(7, result.Microcycles[0].DurationInDays);
        Assert.Null(result.Microcycles[1].TemplateName);
        Assert.Equal("Deload", result.Microcycles[2].TemplateName);
        Assert.Equal(new[] { "Base", "Deload" }, result.AvailableTemplates.Select(t => t.Name));
    }

    [Fact]
    public async Task GetMesocycleDetailQuery_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        Mesocycle mesocycle = CreateActiveMesocycle(Guid.CreateVersion7(), Guid.CreateVersion7());
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        GetMesocycleDetailQueryHandler queryHandler = new GetMesocycleDetailQueryHandler
        (
            _mesocycleRepository,
            Substitute.For<IMicrocycleRepository>(),
            Substitute.For<IProgramRepository>()
        );
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => queryHandler.HandleAsync(new GetMesocycleDetailQuery(mesocycle.Id,Guid.CreateVersion7()),default)
        );
    }

    [Fact]
    public async Task CreateProgramCommand_WithMuscleGroupNamesAsReturnedByTheApi_ParsesThemIgnoringCase()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        CreateProgramCommandHandler commandHandler = new CreateProgramCommandHandler(_programRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new CreateProgramCommand
        (
            Guid.CreateVersion7(),
            "ValidName",
            "Linear",
            new List<string> { "Calves" }
        ),default);
        // Assert
        await _programRepository.Received(1).AddAsync
        (
            Arg.Is<Program>(p => p.AimMuscleGroups.Single() == MuscleGroups.calves),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetUserProgramsQuery_ReturnsTheProgramsOfTheUserWithTheirActiveTemplatesCount()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        Program program = Program.Create(userId, Periodization.Ondulating, "Hipertrofia", new List<MuscleGroups> { MuscleGroups.Back });
        TrainingTemplate template1 = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        TrainingTemplate template2 = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MAV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(template1);
        program.AddTrainingTemplate(template2);
        program.DeleteTrainingTemplate(template2.Id, DateTime.UtcNow);
        _programRepository.GetAllProgramsOfAUserAsync(userId).Returns(new List<Program> { program });
        GetUserProgramsQueryHandler queryHandler = new GetUserProgramsQueryHandler(_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetUserProgramsQuery(userId),default);
        // Assert
        var response = Assert.Single(result);
        Assert.Equal(program.Id, response.Id);
        Assert.Equal("Hipertrofia", response.Name);
        Assert.Equal("Ondulating", response.Periodization);
        Assert.Equal(new[] { "Back" }, response.AimMuscleGroups);
        Assert.Equal(1, response.TrainingTemplatesCount);
    }
}
