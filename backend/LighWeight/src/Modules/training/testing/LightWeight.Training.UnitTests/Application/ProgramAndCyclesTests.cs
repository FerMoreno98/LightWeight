using LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;
using LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;
using LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;
using LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;
using LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;
using LightWeight.Training.Application.Commands.Microcycles.FinishMicrocycle;
using LightWeight.Training.Application.Queries.Macrocycles.GetMacrocycleDetail;
using LightWeight.Training.Application.Queries.Macrocycles.GetUserMacrocycles;
using LightWeight.Training.Application.Queries.Mesocycles.GetMesocycleDetail;
using LightWeight.Training.Application.Queries.Microcycles.GetCurrentMicrocycle;
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

    private static (Program, TrainingTemplate) CreateProgramWithTemplate(Guid userId)
    {
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create("ValidTemplateName",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(template);
        return (program, template);
    }

    private static Macrocycle CreateActiveMacrocycle(Guid userId)
    {
        return Macrocycle.Create(userId, TrainingStage.Bulk, null, DateTime.UtcNow, null);
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
    public async Task FinishMacrocycleCommand_FinishesTheMacrocycleItsActiveMesocycleAndWeek()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), DateTime.UtcNow);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        FinishMacrocycleCommandHandler commandHandler = new FinishMacrocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new FinishMacrocycleCommand(macrocycle.Id,userId),default);
        // Assert
        Assert.True(macrocycle.IsFinished);
        Assert.Equal(macrocycle.FinishedAt, mesocycle.FinishedAt);
        Assert.Equal(macrocycle.FinishedAt, week.FinishedAt);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMacrocycleCommand_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        FinishMacrocycleCommandHandler commandHandler = new FinishMacrocycleCommandHandler(_macrocycleRepository,_UOW);
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
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        _macrocycleRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Macrocycle?)null);
        FinishMacrocycleCommandHandler commandHandler = new FinishMacrocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<MacrocycleNotFoundException>
        (
            () => commandHandler.HandleAsync(new FinishMacrocycleCommand(Guid.CreateVersion7(),Guid.CreateVersion7()),default)
        );
    }

    // ---------------------------------------------------------------- Mesocycles

    [Fact]
    public async Task CreateMesocycleCommand_WithValidData_AddsTheMesocycleToTheMacrocycle()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Program program = CreateProgram(userId);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        Guid id = await commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,program.Id,8,"Hombro",null),default);
        // Assert
        Mesocycle mesocycle = Assert.Single(macrocycle.Mesocycles);
        Assert.Equal(id, mesocycle.Id);
        Assert.Equal(program.Id, mesocycle.ProgramId);
        Assert.Equal("Hombro", mesocycle.Injuries);
        Assert.False(mesocycle.IsFinished);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMesocycleCommand_WithAnActiveMesocycle_ThrowsDomainExceptionAndDoesNotSave()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Program program = CreateProgram(userId);
        macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
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
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,Guid.CreateVersion7(),8,null,null),default)
        );
    }

    [Fact]
    public async Task CreateMesocycleCommand_ProgramOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Program program = CreateProgram(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,program.Id,8,null,null),default)
        );
        Assert.Empty(macrocycle.Mesocycles);
    }

    [Fact]
    public async Task CreateMesocycleCommand_ProgramNotFound_ThrowApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Program?)null);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<ProgramNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand(macrocycle.Id,userId,Guid.CreateVersion7(),8,null,null),default)
        );
    }

    [Fact]
    public async Task FinishMesocycleCommand_FinishesTheMesocycleAndItsActiveWeek()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        FinishMesocycleCommandHandler commandHandler = new FinishMesocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new FinishMesocycleCommand(mesocycle.Id,userId),default);
        // Assert
        Assert.True(mesocycle.IsFinished);
        Assert.True(week.IsFinished);
        Assert.False(macrocycle.IsFinished);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMesocycleCommand_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        Mesocycle mesocycle = macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        FinishMesocycleCommandHandler commandHandler = new FinishMesocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new FinishMesocycleCommand(mesocycle.Id,Guid.CreateVersion7()),default)
        );
        Assert.False(mesocycle.IsFinished);
    }

    [Fact]
    public async Task FinishMesocycleCommand_NotFound_ThrowsApplicationException()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        _macrocycleRepository.GetByMesocycleIdAsync(Arg.Any<Guid>()).Returns((Macrocycle?)null);
        FinishMesocycleCommandHandler commandHandler =
            new FinishMesocycleCommandHandler(_macrocycleRepository,Substitute.For<ITrainingUnitOfWork>());
        // Act
        // Assert
        await Assert.ThrowsAsync<MesocycleNotFoundException>
        (
            () => commandHandler.HandleAsync(new FinishMesocycleCommand(Guid.CreateVersion7(),Guid.CreateVersion7()),default)
        );
    }

    // ---------------------------------------------------------------- Microcycles

    [Fact]
    public async Task CreateMicrocycleCommand_WithTemplateOfTheMesocycleProgram_AddsTheFirstWeek()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        Guid id = await commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default);
        // Assert
        Microcycle week = Assert.Single(mesocycle.Microcycles);
        Assert.Equal(id, week.Id);
        Assert.Equal(template.Id, week.TrainingTemplateId);
        Assert.Equal(1, week.WeekNumber);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMicrocycleCommand_AfterFinishingThePreviousWeeks_NumbersTheNextWeek()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        Microcycle week1 = macrocycle.PlanMicrocycle(mesocycle.Id, template.Id, DateTime.UtcNow);
        macrocycle.FinishMicrocycle(week1.Id, DateTime.UtcNow);
        Microcycle week2 = macrocycle.PlanMicrocycle(mesocycle.Id, template.Id, DateTime.UtcNow);
        macrocycle.FinishMicrocycle(week2.Id, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        Guid id = await commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default);
        // Assert
        Assert.Equal(3, mesocycle.Microcycles.Single(m => m.Id == id).WeekNumber);
    }

    [Fact]
    public async Task CreateMicrocycleCommand_WithAnActiveWeek_ThrowsDomainExceptionAndDoesNotSave()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        macrocycle.PlanMicrocycle(mesocycle.Id, template.Id, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<ActiveMicrocycleAlreadyExistsDomainException>
        (
            () => commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMicrocycleCommand_InAFinishedMesocycle_ThrowsDomainExceptionAndDoesNotSave()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        macrocycle.FinishMesocycle(mesocycle.Id, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
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
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
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
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        program.DeleteTrainingTemplate(template.Id, DateTime.UtcNow);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMicrocycleCommand_FinishesOnlyTheWeekAndSaves()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), DateTime.UtcNow);
        _macrocycleRepository.GetByMicrocycleIdAsync(week.Id).Returns(macrocycle);
        FinishMicrocycleCommandHandler commandHandler = new FinishMicrocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new FinishMicrocycleCommand(week.Id,userId),default);
        // Assert
        Assert.True(week.IsFinished);
        Assert.False(mesocycle.IsFinished);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMicrocycleCommand_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        Mesocycle mesocycle = macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), DateTime.UtcNow);
        _macrocycleRepository.GetByMicrocycleIdAsync(week.Id).Returns(macrocycle);
        FinishMicrocycleCommandHandler commandHandler = new FinishMicrocycleCommandHandler(_macrocycleRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new FinishMicrocycleCommand(week.Id,Guid.CreateVersion7()),default)
        );
        Assert.False(week.IsFinished);
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishMicrocycleCommand_NotFound_ThrowsApplicationException()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        _macrocycleRepository.GetByMicrocycleIdAsync(Arg.Any<Guid>()).Returns((Macrocycle?)null);
        FinishMicrocycleCommandHandler commandHandler =
            new FinishMicrocycleCommandHandler(_macrocycleRepository,Substitute.For<ITrainingUnitOfWork>());
        // Act
        // Assert
        await Assert.ThrowsAsync<MicrocycleNotFoundException>
        (
            () => commandHandler.HandleAsync(new FinishMicrocycleCommand(Guid.CreateVersion7(),Guid.CreateVersion7()),default)
        );
    }

    // ---------------------------------------------------------------- Cycle queries

    [Fact]
    public async Task GetUserMacrocyclesQuery_ReturnsTheActiveFirstThenTheFinishedFromTheMostRecent()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        Macrocycle oldest = Macrocycle.Create(userId, TrainingStage.Bulk, null, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), null);
        oldest.Finish(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        Macrocycle older = Macrocycle.Create(userId, TrainingStage.Cut, null, new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc), null);
        Mesocycle olderMesocycle1 = older.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        older.FinishMesocycle(olderMesocycle1.Id, new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        older.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, new DateTime(2025, 7, 2, 0, 0, 0, DateTimeKind.Utc));
        older.Finish(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));
        Macrocycle active = Macrocycle.Create(userId, TrainingStage.Maintenance, "Actual", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null);
        _macrocycleRepository.GetAllOfAUserAsync(userId).Returns(new List<Macrocycle> { oldest, active, older });
        GetUserMacrocyclesQueryHandler queryHandler = new GetUserMacrocyclesQueryHandler(_macrocycleRepository);
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
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle first = macrocycle.PlanMesocycle(program.Id, 7, "Rodilla", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        macrocycle.PlanMicrocycle(first.Id, template.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        macrocycle.FinishMesocycle(first.Id, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        Mesocycle second = macrocycle.PlanMesocycle(program.Id, 9, null, null, new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc));
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetAllProgramsOfAUserAsync(userId).Returns(new List<Program> { program });
        GetMacrocycleDetailQueryHandler queryHandler =
            new GetMacrocycleDetailQueryHandler(_macrocycleRepository,_programRepository);
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
        GetMacrocycleDetailQueryHandler queryHandler =
            new GetMacrocycleDetailQueryHandler(_macrocycleRepository,Substitute.For<IProgramRepository>());
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
        GetMacrocycleDetailQueryHandler queryHandler =
            new GetMacrocycleDetailQueryHandler(_macrocycleRepository,Substitute.For<IProgramRepository>());
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
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        Program program = CreateProgram(userId);
        TrainingTemplate baseTemplate = TrainingTemplate.Create("Base",VolumeLandmarks.MEV,TrainingDistribution.FullBody,7);
        TrainingTemplate deload = TrainingTemplate.Create("Deload",VolumeLandmarks.MV,TrainingDistribution.FullBody,5);
        TrainingTemplate removed = TrainingTemplate.Create("Removed",VolumeLandmarks.MAV,TrainingDistribution.FullBody,7);
        program.AddTrainingTemplate(baseTemplate);
        program.AddTrainingTemplate(deload);
        program.AddTrainingTemplate(removed);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        foreach(var template in new[] { baseTemplate, removed })
        {
            Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, template.Id, DateTime.UtcNow);
            macrocycle.FinishMicrocycle(week.Id, DateTime.UtcNow);
        }
        macrocycle.PlanMicrocycle(mesocycle.Id, deload.Id, DateTime.UtcNow);
        program.DeleteTrainingTemplate(removed.Id, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        GetMesocycleDetailQueryHandler queryHandler =
            new GetMesocycleDetailQueryHandler(_macrocycleRepository,_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetMesocycleDetailQuery(mesocycle.Id,userId),default);
        // Assert
        Assert.Equal("ValidProgramName", result.ProgramName);
        Assert.Equal(macrocycle.Id, result.MacrocycleId);
        Assert.Equal(new[] { 1, 2, 3 }, result.Microcycles.Select(m => m.WeekNumber));
        Assert.Equal("Base", result.Microcycles[0].TemplateName);
        Assert.Equal(7, result.Microcycles[0].DurationInDays);
        Assert.NotNull(result.Microcycles[0].FinishedAt);
        Assert.Null(result.Microcycles[1].TemplateName);
        Assert.Equal("Deload", result.Microcycles[2].TemplateName);
        Assert.Null(result.Microcycles[2].FinishedAt);
        Assert.Equal(new[] { "Base", "Deload" }, result.AvailableTemplates.Select(t => t.Name));
    }

    [Fact]
    public async Task GetMesocycleDetailQuery_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        Macrocycle macrocycle = CreateActiveMacrocycle(Guid.CreateVersion7());
        Mesocycle mesocycle = macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        _macrocycleRepository.GetByMesocycleIdAsync(mesocycle.Id).Returns(macrocycle);
        GetMesocycleDetailQueryHandler queryHandler =
            new GetMesocycleDetailQueryHandler(_macrocycleRepository,Substitute.For<IProgramRepository>());
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => queryHandler.HandleAsync(new GetMesocycleDetailQuery(mesocycle.Id,Guid.CreateVersion7()),default)
        );
    }

    // ---------------------------------------------------------------- Current microcycle

    [Fact]
    public async Task GetCurrentMicrocycleQuery_WithoutActiveMacrocycle_ReturnsNoActiveMacrocycle()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        _macrocycleRepository.GetActiveOfAUserAsync(userId).Returns((Macrocycle?)null);
        GetCurrentMicrocycleQueryHandler queryHandler =
            new GetCurrentMicrocycleQueryHandler(_macrocycleRepository,Substitute.For<IProgramRepository>());
        // Act
        var result = await queryHandler.HandleAsync(new GetCurrentMicrocycleQuery(userId),default);
        // Assert
        Assert.Equal(CurrentMicrocycleStatus.NoActiveMacrocycle, result.Status);
        Assert.Null(result.MacrocycleId);
        Assert.Null(result.Microcycle);
    }

    [Fact]
    public async Task GetCurrentMicrocycleQuery_WithoutActiveMesocycle_ReturnsNoActiveMesocycle()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle finished = macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, DateTime.UtcNow);
        macrocycle.FinishMesocycle(finished.Id, DateTime.UtcNow);
        _macrocycleRepository.GetActiveOfAUserAsync(userId).Returns(macrocycle);
        GetCurrentMicrocycleQueryHandler queryHandler =
            new GetCurrentMicrocycleQueryHandler(_macrocycleRepository,Substitute.For<IProgramRepository>());
        // Act
        var result = await queryHandler.HandleAsync(new GetCurrentMicrocycleQuery(userId),default);
        // Assert
        Assert.Equal(CurrentMicrocycleStatus.NoActiveMesocycle, result.Status);
        Assert.Equal(macrocycle.Id, result.MacrocycleId);
        Assert.Null(result.MesocycleId);
    }

    [Fact]
    public async Task GetCurrentMicrocycleQuery_WithoutActiveWeek_ReturnsPendingPlanningWithTheNextWeek()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        Microcycle week1 = macrocycle.PlanMicrocycle(mesocycle.Id, template.Id, DateTime.UtcNow);
        macrocycle.FinishMicrocycle(week1.Id, DateTime.UtcNow);
        _macrocycleRepository.GetActiveOfAUserAsync(userId).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        GetCurrentMicrocycleQueryHandler queryHandler =
            new GetCurrentMicrocycleQueryHandler(_macrocycleRepository,_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetCurrentMicrocycleQuery(userId),default);
        // Assert
        Assert.Equal(CurrentMicrocycleStatus.PendingPlanning, result.Status);
        Assert.Equal(mesocycle.Id, result.MesocycleId);
        Assert.Equal(program.Id, result.ProgramId);
        Assert.Equal("ValidProgramName", result.ProgramName);
        Assert.Equal(2, result.NextWeekNumber);
        Assert.Null(result.Microcycle);
    }

    [Fact]
    public async Task GetCurrentMicrocycleQuery_WithAnActiveWeek_ReturnsItWithItsTemplate()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, template.Id, DateTime.UtcNow);
        _macrocycleRepository.GetActiveOfAUserAsync(userId).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        GetCurrentMicrocycleQueryHandler queryHandler =
            new GetCurrentMicrocycleQueryHandler(_macrocycleRepository,_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetCurrentMicrocycleQuery(userId),default);
        // Assert
        Assert.Equal(CurrentMicrocycleStatus.InProgress, result.Status);
        Assert.Null(result.NextWeekNumber);
        Assert.NotNull(result.Microcycle);
        Assert.Equal(week.Id, result.Microcycle.Id);
        Assert.Equal(1, result.Microcycle.WeekNumber);
        Assert.Equal(template.Id, result.Microcycle.TrainingTemplateId);
        Assert.Equal("ValidTemplateName", result.Microcycle.TemplateName);
        Assert.Equal(7, result.Microcycle.DurationInDays);
    }

    [Fact]
    public async Task GetCurrentMicrocycleQuery_WithADeletedTemplate_ReturnsTheWeekWithoutTemplateName()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        Macrocycle macrocycle = CreateActiveMacrocycle(userId);
        Mesocycle mesocycle = macrocycle.PlanMesocycle(program.Id, 8, null, null, DateTime.UtcNow);
        macrocycle.PlanMicrocycle(mesocycle.Id, template.Id, DateTime.UtcNow);
        program.DeleteTrainingTemplate(template.Id, DateTime.UtcNow);
        _macrocycleRepository.GetActiveOfAUserAsync(userId).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        GetCurrentMicrocycleQueryHandler queryHandler =
            new GetCurrentMicrocycleQueryHandler(_macrocycleRepository,_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetCurrentMicrocycleQuery(userId),default);
        // Assert
        Assert.Equal(CurrentMicrocycleStatus.InProgress, result.Status);
        Assert.NotNull(result.Microcycle);
        Assert.Null(result.Microcycle.TemplateName);
        Assert.Null(result.Microcycle.DurationInDays);
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
