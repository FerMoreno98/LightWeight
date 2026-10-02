using LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;
using LightWeight.Training.Application.Commands.TemplateSets.DeleteTemplateSet;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class SetTemplateTests
{
    private static (Program program, TemplateSession session) CreateProgramWithSession(Guid userId, bool addSession = true)
    {
        Program program = Program.Create
        (
            userId,
            Periodization.Linear,
            "ValidProgramName",
            new List<MuscleGroups>()
        );
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            VolumeLandmarks.MAV,
            TrainingDistribution.FullBody,
            7,
            1
        );
        TemplateSession sessionTemplate = TemplateSession.Create
        (
            "ValidSessionName"
        );
        if (addSession)
            trainingTemplate.AddSessionTemplate(sessionTemplate);
        program.AddTrainingTemplate(trainingTemplate);
        return (program, sessionTemplate);
    }

    private static CreateTemplateSetCommand CreateCommand(Guid exerciseId, Guid sessionId, Guid userId, int series = 1)
    {
        return new CreateTemplateSetCommand
        (
            exerciseId,
            sessionId,
            userId,
            6,
            8,
            false,
            false,
            false,
            8,
            series,
            new List<string>(),
            null
        );
    }

    [Fact]
    public async Task CreateTemplateSetCommand_SetAddedToASessionCorrectly()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        IProgramRepository _programRepository =
            Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW =
            Substitute.For<ITrainingUnitOfWork>();
        IExerciseRepository _exerciseRepository =
            Substitute.For<IExerciseRepository>();
        Exercise exercise = Exercise.Create
        (
            "ValidExerciseName",
            true,
            new List<MuscleGroups>()
        );
        _exerciseRepository.GetByIdAsync(exercise.Id).Returns(exercise);
        var (program, sessionTemplate) = CreateProgramWithSession(UserId);
        _programRepository
            .GetByTemplateSessionIdAsync(sessionTemplate.Id)
                .Returns(program);

        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _programRepository,
            _UOW,
            _exerciseRepository
        );
        // Act
        await commandHandler.HandleAsync(CreateCommand(exercise.Id, sessionTemplate.Id, UserId, series: 3),default);
        // Assert
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(3, sessionTemplate.TemplateExercises.Count);
        Assert.All(sessionTemplate.TemplateExercises, s => Assert.Equal(exercise.Id, s.ExerciseId));
        Assert.All(sessionTemplate.TemplateExercises, s => Assert.Equal(8, s.ExpectedRPE));
    }


    [Fact]
    public async Task CreateTemplateSetCommand_ExerciseNotFound_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        var fakeExerciseId = Guid.CreateVersion7();
        var fakeSessionId = Guid.CreateVersion7();
        IProgramRepository _programRepository =
            Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW =
            Substitute.For<ITrainingUnitOfWork>();
        IExerciseRepository _exerciseRepository =
            Substitute.For<IExerciseRepository>();

        _exerciseRepository.GetByIdAsync(fakeExerciseId)
            .Returns((Exercise?) null);
        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _programRepository,
            _UOW,
            _exerciseRepository
        );
        // Act
        // Assert
       await Assert.ThrowsAsync<ExerciseNotFounApplicationException>
        (
            () =>
            commandHandler.HandleAsync(CreateCommand(fakeExerciseId, fakeSessionId, UserId, series: 2),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplateSetCommand_ProgramNotFound_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        var fakeSessionTemplateId = Guid.CreateVersion7();
        IProgramRepository _programRepository =
            Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW =
            Substitute.For<ITrainingUnitOfWork>();
        IExerciseRepository _exerciseRepository =
            Substitute.For<IExerciseRepository>();
        Exercise exercise = Exercise.Create
        (
            "ValidExerciseName",
            true,
            new List<MuscleGroups>()
        );
        _exerciseRepository.GetByIdAsync(exercise.Id).Returns(exercise);

        _programRepository
            .GetByTemplateSessionIdAsync(fakeSessionTemplateId)
                .Returns((Program?) null);

        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _programRepository,
            _UOW,
            _exerciseRepository
        );
        // Act
        // Assert
        await Assert.ThrowsAsync<TemplateSessionNotFoundApplicationException>
        (
            () =>
            commandHandler.HandleAsync(CreateCommand(exercise.Id, fakeSessionTemplateId, UserId),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplateSetCommand_UserIdDoesNotCorrespondWithProgramUserId_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        IProgramRepository _programRepository =
            Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW =
            Substitute.For<ITrainingUnitOfWork>();
        IExerciseRepository _exerciseRepository =
            Substitute.For<IExerciseRepository>();
        Exercise exercise = Exercise.Create
        (
            "ValidExerciseName",
            true,
            new List<MuscleGroups>()
        );
        _exerciseRepository.GetByIdAsync(exercise.Id).Returns(exercise);
        var (program, sessionTemplate) = CreateProgramWithSession(Guid.CreateVersion7());
        _programRepository
            .GetByTemplateSessionIdAsync(sessionTemplate.Id)
                .Returns(program);

        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _programRepository,
            _UOW,
            _exerciseRepository
        );
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () =>
            commandHandler.HandleAsync(CreateCommand(exercise.Id, sessionTemplate.Id, UserId),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplateSetCommand_SessionNotFoundInProgram_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        IProgramRepository _programRepository =
            Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW =
            Substitute.For<ITrainingUnitOfWork>();
        IExerciseRepository _exerciseRepository =
            Substitute.For<IExerciseRepository>();
        Exercise exercise = Exercise.Create
        (
            "ValidExerciseName",
            true,
            new List<MuscleGroups>()
        );
        _exerciseRepository.GetByIdAsync(exercise.Id).Returns(exercise);
        var (program, sessionTemplate) = CreateProgramWithSession(UserId, addSession: false);
        _programRepository
            .GetByTemplateSessionIdAsync(sessionTemplate.Id)
                .Returns(program);

        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _programRepository,
            _UOW,
            _exerciseRepository
        );
        // Act
        // Assert
        await Assert.ThrowsAsync<TemplateSessionNotFoundApplicationException>
        (
            () =>
            commandHandler.HandleAsync(CreateCommand(exercise.Id, sessionTemplate.Id, UserId),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTemplateSetCommand_WhenHappyPath_SoftDeletesTheSet()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        var (program, sessionTemplate) = CreateProgramWithSession(UserId);
        TemplateSet set = TemplateSet.Create
        (
            Guid.CreateVersion7(),
            RepetitionRange.Create(8,6),
            8,
            new List<MuscleGroups> { MuscleGroups.Back }
        );
        sessionTemplate.AddSet(set);
        _programRepository
            .GetByTemplateSetIdAsync(set.Id)
                .Returns(program);
        DeleteTemplateSetCommandHandler commandHandler =
            new DeleteTemplateSetCommandHandler(_programRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new DeleteTemplateSetCommand(set.Id,UserId),default);
        // Assert
        Assert.True(set.IsDeleted);
        Assert.Empty(sessionTemplate.TemplateExercises);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTemplateSetCommand_ProgramNotFound_ThrowApplicationException()
    {
        // Arrange
        var fakeSetId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        _programRepository
            .GetByTemplateSetIdAsync(fakeSetId)
                .Returns((Program?) null);
        DeleteTemplateSetCommandHandler commandHandler =
            new DeleteTemplateSetCommandHandler(_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<TemplateSetNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new DeleteTemplateSetCommand(fakeSetId,Guid.CreateVersion7()),default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
