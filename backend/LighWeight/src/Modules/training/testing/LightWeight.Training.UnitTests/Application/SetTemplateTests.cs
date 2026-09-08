using System.Windows.Markup;
using LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class SetTemplateTests
{
    [Fact]
    public async Task CreateTemplateSetCommand_SetAddedToASessionCorrectly()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository =
            Substitute.For<ITrainingTemplateRepository>();
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
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            UserId,
            "ValidName",
            Training.Domain.Enum.VolumeLandmarks.MAV,
            Training.Domain.Enum.TrainingDistribution.FullBody
        );
 
        TemplateSession sessionTemplate = TemplateSession.Create
        (
            "ValidSessionName"
        );
        trainingTemplate.AddSessionTemplate(sessionTemplate);
        _trainingTemplateRepository
            .GetBySessionIdAsync(sessionTemplate.Id)
                .Returns(trainingTemplate);

        CreateTemplateSetCommand command = new CreateTemplateSetCommand
        (
            exercise.Id,
            sessionTemplate.Id,
            UserId,
            6,
            8,
            false,
            false,
            false,
            2,
            1,
            new List<string>(),
            null
        );
        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _trainingTemplateRepository,
            _UOW,
            _exerciseRepository
            
        );
        // Act
        await commandHandler.HandleAsync(command,default);
        // Assert 
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Contains(sessionTemplate.TemplateExercises, s => s.ExerciseId == exercise.Id );
    }


    [Fact]
    public async Task CreateTemplateSetCommand_ExerciseNotFound_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        var fakeExerciseId = Guid.CreateVersion7();
        var fakeSessionId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository =
            Substitute.For<ITrainingTemplateRepository>();
        ITrainingUnitOfWork _UOW = 
            Substitute.For<ITrainingUnitOfWork>();
        IExerciseRepository _exerciseRepository = 
            Substitute.For<IExerciseRepository>();

        _exerciseRepository.GetByIdAsync(fakeExerciseId)
            .Returns((Exercise?) null);
        CreateTemplateSetCommand command = new CreateTemplateSetCommand
        (
            fakeExerciseId,
            fakeSessionId,
            UserId,
            6,
            8,
            false,
            false,
            false,
            2,
            2,
            new List<string>(),
            null
        );
        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _trainingTemplateRepository,
            _UOW,
            _exerciseRepository
            
        );
        // Act
        // Assert 
       await Assert.ThrowsAsync<ExerciseNotFounApplicationException>
        (
            () =>
            commandHandler.HandleAsync(command,default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplateSetCommand_TemplateNotFound_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        var fakeTrainingTemplateId = Guid.CreateVersion7();
        var fakeSessionTemplateId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository =
            Substitute.For<ITrainingTemplateRepository>();
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

        _trainingTemplateRepository
            .GetBySessionIdAsync(fakeTrainingTemplateId)
                .Returns((TrainingTemplate?) null);

        CreateTemplateSetCommand command = new CreateTemplateSetCommand
        (
            exercise.Id,
            fakeSessionTemplateId,
            UserId,
            6,
            8,
            false,
            false,
            false,
            2,
            1,
            new List<string>(),
            null
        );
        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _trainingTemplateRepository,
            _UOW,
            _exerciseRepository
            
        );
        // Act
        // Assert 
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () =>
            commandHandler.HandleAsync(command,default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplateSetCommand_UserIdDoesNotCorrespondWithTemplateUserId_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository =
            Substitute.For<ITrainingTemplateRepository>();
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
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            Guid.CreateVersion7(),
            "ValidName",
            Training.Domain.Enum.VolumeLandmarks.MAV,
            Training.Domain.Enum.TrainingDistribution.FullBody
        );
 
        TemplateSession sessionTemplate = TemplateSession.Create
        (
            "ValidSessionName"
        );
        trainingTemplate.AddSessionTemplate(sessionTemplate);
        _trainingTemplateRepository
            .GetBySessionIdAsync(sessionTemplate.Id)
                .Returns(trainingTemplate);

        CreateTemplateSetCommand command = new CreateTemplateSetCommand
        (
            exercise.Id,
            sessionTemplate.Id,
            UserId,
            6,
            8,
            false,
            false,
            false,
            2,
            1,
            new List<string>(),
            null
        );
        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _trainingTemplateRepository,
            _UOW,
            _exerciseRepository
            
        );
        // Act
        // Assert 
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () =>
            commandHandler.HandleAsync(command,default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task CreateTemplateSetCommand_SessionNotFoundInTrainingTemplate_ThrowApplicationException()
    {
        // Arrange
        var UserId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository =
            Substitute.For<ITrainingTemplateRepository>();
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
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            UserId,
            "ValidName",
            Training.Domain.Enum.VolumeLandmarks.MAV,
            Training.Domain.Enum.TrainingDistribution.FullBody
        );
 
        TemplateSession sessionTemplate = TemplateSession.Create
        (
            "ValidSessionName"
        );
        _trainingTemplateRepository
            .GetBySessionIdAsync(sessionTemplate.Id)
                .Returns(trainingTemplate);

        CreateTemplateSetCommand command = new CreateTemplateSetCommand
        (
            exercise.Id,
            sessionTemplate.Id,
            UserId,
            6,
            8,
            false,
            false,
            false,
            2,
            1,
            new List<string>(),
            null
        );
        CreateTemplateSetCommandHandler commandHandler = new CreateTemplateSetCommandHandler
        (
            _trainingTemplateRepository,
            _UOW,
            _exerciseRepository
            
        );
        // Act
        // Assert 
        await Assert.ThrowsAsync<TemplateSessionNotFoundApplicationException>
        (
            () =>
            commandHandler.HandleAsync(command,default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    
}