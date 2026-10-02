using LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;
using LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;
using LightWeight.Training.Application.Commands.Programs.CreateProgram;
using LightWeight.Training.Application.Exceptions;
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

    [Fact]
    public async Task CreateMesocycleCommand_WithValidData_AddsTheMesocycleLinkedToTheProgram()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMacrocycleRepository _macrocycleRepository = Substitute.For<IMacrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Macrocycle macrocycle = Macrocycle.Create(userId, DateTime.UtcNow, null, TrainingStage.Bulk, null);
        Program program = CreateProgram(userId);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new CreateMesocycleCommand
        (
            macrocycle.Id,
            userId,
            program.Id,
            8,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(28)
        ),default);
        // Assert
        await _mesocycleRepository.Received(1).AddAsync
        (
            Arg.Is<Mesocycle>(m => m.ProgramId == program.Id && m.MacrocycleId == macrocycle.Id),
            Arg.Any<CancellationToken>()
        );
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
        Macrocycle macrocycle = Macrocycle.Create(userId, DateTime.UtcNow, null, TrainingStage.Bulk, null);
        Program program = CreateProgram(Guid.CreateVersion7());
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand
            (
                macrocycle.Id,
                userId,
                program.Id,
                8,
                null,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(28)
            ),default)
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
        Macrocycle macrocycle = Macrocycle.Create(userId, DateTime.UtcNow, null, TrainingStage.Bulk, null);
        _macrocycleRepository.GetByIdAsync(macrocycle.Id).Returns(macrocycle);
        _programRepository.GetByIdAsync(fakeProgramId).Returns((Program?) null);
        CreateMesocycleCommandHandler commandHandler =
            new CreateMesocycleCommandHandler(_mesocycleRepository,_macrocycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<ProgramNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMesocycleCommand
            (
                macrocycle.Id,
                userId,
                fakeProgramId,
                8,
                null,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(28)
            ),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMicrocycleCommand_WithTemplateOfTheMesocycleProgram_AddsTheMicrocycle()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IMesocycleRepository _mesocycleRepository = Substitute.For<IMesocycleRepository>();
        IMicrocycleRepository _microcycleRepository = Substitute.For<IMicrocycleRepository>();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create(VolumeLandmarks.MEV,TrainingDistribution.FullBody,7,1);
        program.AddTrainingTemplate(template);
        Mesocycle mesocycle = Mesocycle.Create(Guid.CreateVersion7(),userId,8,null,null,DateTime.UtcNow,DateTime.UtcNow.AddDays(28),program.Id);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id,1),default);
        // Assert
        await _microcycleRepository.Received(1).AddAsync
        (
            Arg.Is<Microcycle>(m => m.TrainingTemplateId == template.Id && m.WeekNumber == 1),
            Arg.Any<CancellationToken>()
        );
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
        Mesocycle mesocycle = Mesocycle.Create(Guid.CreateVersion7(),userId,8,null,null,DateTime.UtcNow,DateTime.UtcNow.AddDays(28),program.Id);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,Guid.CreateVersion7(),1),default)
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
        TrainingTemplate template = TrainingTemplate.Create(VolumeLandmarks.MEV,TrainingDistribution.FullBody,7,1);
        program.AddTrainingTemplate(template);
        program.DeleteTrainingTemplate(template.Id, DateTime.UtcNow);
        Mesocycle mesocycle = Mesocycle.Create(Guid.CreateVersion7(),userId,8,null,null,DateTime.UtcNow,DateTime.UtcNow.AddDays(28),program.Id);
        _mesocycleRepository.GetByIdAsync(mesocycle.Id).Returns(mesocycle);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateMicrocycleCommandHandler commandHandler =
            new CreateMicrocycleCommandHandler(_mesocycleRepository,_microcycleRepository,_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateMicrocycleCommand(mesocycle.Id,userId,template.Id,1),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
