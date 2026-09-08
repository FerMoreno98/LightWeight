using System.Windows.Markup;
using LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;
using LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;
using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Queries.SessionTemplates.GetNumberOfSeriesPerGroupPerSession;
using LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class SessionTemplateTests
{
    [Theory]
    [InlineData("validName")]
    [InlineData("12345")]
    [InlineData("__??")]
    public async Task CreateTemplateSession_WhenHappyPath_ReturnsTemplateId(string name)
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository = Substitute.For<ITrainingTemplateRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            userId,
            name,
            Training.Domain.Enum.VolumeLandmarks.MAV,
            Training.Domain.Enum.TrainingDistribution.FullBody
        );
         _trainingTemplateRepository
            .GetByIdAsync(trainingTemplate.Id)
                .Returns(trainingTemplate);
        CreateTemplateSessionCommand command = new CreateTemplateSessionCommand
        (
            trainingTemplate.Id,
            userId,
            name
        );
        CreateTemplateSessionCommandHandler commandHandler = new CreateTemplateSessionCommandHandler
        (
            _trainingTemplateRepository,
            _UOW
        );
        // Act
        Guid TemplateId = await commandHandler.HandleAsync(command,default);
        // Assert
        Assert.NotEqual(TemplateId,Guid.Empty);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Contains(trainingTemplate.TemplateSessions, s => s.Name == name);
    }

    [Fact]
    public async Task CreateTemplateSessionCommand_WhenTrainingTemplateDoesNotExists_ReturnsApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var templateId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository = Substitute.For<ITrainingTemplateRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        _trainingTemplateRepository
            .GetByIdAsync(templateId)
                .Returns((TrainingTemplate?) null);
        CreateTemplateSessionCommand command =
            new CreateTemplateSessionCommand(templateId,userId,"validName");
        CreateTemplateSessionCommandHandler commandHandler =
             new CreateTemplateSessionCommandHandler(_trainingTemplateRepository,_UOW);
        // Act

        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
             () =>
                commandHandler.HandleAsync(command, default)
        );
    }
    
    [Fact]
    public async Task CreateTemplateSessionCommand_WhenTrainingTemplateUserIdDoesNotMatchWithUserId_ReturnsApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository = Substitute.For<ITrainingTemplateRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            Guid.CreateVersion7(),
            "validName",
            Training.Domain.Enum.VolumeLandmarks.MAV,
            Training.Domain.Enum.TrainingDistribution.FullBody

        );
        _trainingTemplateRepository
            .GetByIdAsync(trainingTemplate.Id)
                .Returns(trainingTemplate);
        CreateTemplateSessionCommand command =
            new CreateTemplateSessionCommand(trainingTemplate.Id,userId,"validName");
        CreateTemplateSessionCommandHandler commandHandler =
             new CreateTemplateSessionCommandHandler(_trainingTemplateRepository,_UOW);
        // Act

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
             () =>
                commandHandler.HandleAsync(command, default)
        );
    }
    [Fact]
    public async Task GetNumberOfSeriesPerGroupPerSession_ReturnSeriesSuccessfully()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        ITrainingTemplateRepository _trainingTemplateRepository = Substitute.For<ITrainingTemplateRepository>();
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            userId,
            "ValidName",
            Training.Domain.Enum.VolumeLandmarks.MAV,
            Training.Domain.Enum.TrainingDistribution.FullBody
        );
        TemplateSession templateSession = TemplateSession.Create
        (
            "ValidName"
        );
        List<MuscleGroups> muscleGroups = new List<MuscleGroups>
        {
            MuscleGroups.Back,
            MuscleGroups.Biceps,
            MuscleGroups.Biceps,
            MuscleGroups.Chest
        };

        Exercise exercise = Exercise.Create
        (
            "exercis1",
            true,
            muscleGroups
        );
        Exercise exercise2 = Exercise.Create
        (
            "exercis2",
            true,
            new List<MuscleGroups>()
        );
        Exercise exercise3 = Exercise.Create
        (
            "exercis3",
            true,
            muscleGroups
        );
        AdvanceTrainingTechniques advanceTrainingTechniques = AdvanceTrainingTechniques.Create(false,false,false);
        RepetitionRange repetitionRange = RepetitionRange.Create(6,8);
        TemplateSet set1 = TemplateSet.Create
        (
            exercise.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        TemplateSet set2 = TemplateSet.Create
        (
            exercise2.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        TemplateSet set3 = TemplateSet.Create
        (
            exercise3.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        templateSession.AddSet(set1);
        templateSession.AddSet(set2);
        templateSession.AddSet(set3);
        trainingTemplate.AddSessionTemplate(templateSession);
        _trainingTemplateRepository
            .GetByIdAsync(trainingTemplate.Id)
                .Returns(trainingTemplate);
        GetNumberOfSeriesPerGroupPerSessionQuery query =
            new GetNumberOfSeriesPerGroupPerSessionQuery
            (
               trainingTemplate.Id
            );
        GetNumberOfSeriesPerGroupPerSessionQueryHandler queryHandler = 
            new GetNumberOfSeriesPerGroupPerSessionQueryHandler(_trainingTemplateRepository); 
        // Act
        var result = await queryHandler.HandleAsync(query,default);
        // Assert
        Assert.Equal(result[Random.Shared.Next(result.Count)].SessionId,templateSession.Id);
        Assert.All(result, r => Assert.Equal(6, r.Series["Biceps"]));
        Assert.All(result, r => Assert.Equal(3, r.Series["Back"]));
        Assert.All(result, r => Assert.Equal(3, r.Series["Chest"]));
        

    }
    // [Fact]
    // public async Task GetSetsFromSessionTemplate_ReturnsSeriesSuccessfully()
    // {
    //     var userId = Guid.CreateVersion7();
    //     ITrainingTemplateRepository _trainingTemplateRepository = Substitute.For<ITrainingTemplateRepository>();
    //     TrainingTemplate trainingTemplate = TrainingTemplate.Create
    //     (
    //         userId,
    //         "ValidName",
    //         Training.Domain.Enum.VolumeLandmarks.MAV,
    //         Training.Domain.Enum.TrainingDistribution.FullBody
    //     );
    //     TemplateSession templateSession = TemplateSession.Create
    //     (
    //         "ValidName"
    //     );
    //     List<MuscleGroups> muscleGroups = new List<MuscleGroups>
    //     {
    //         MuscleGroups.Back,
    //         MuscleGroups.Biceps,
    //         MuscleGroups.Biceps,
    //         MuscleGroups.Chest
    //     };

    //     Exercise exercise = Exercise.Create
    //     (
    //         "exercis1",
    //         true,
    //         muscleGroups
    //     );
    //     Exercise exercise2 = Exercise.Create
    //     (
    //         "exercis2",
    //         true,
    //         new List<MuscleGroups>()
    //     );
    //     Exercise exercise3 = Exercise.Create
    //     (
    //         "exercis3",
    //         true,
    //         muscleGroups
    //     );
    //     AdvanceTrainingTechniques advanceTrainingTechniques = AdvanceTrainingTechniques.Create(false,false,false);
    //     RepetitionRange repetitionRange = RepetitionRange.Create(6,8);
    //     TemplateSet set1 = TemplateSet.Create
    //     (
    //         exercise.Id,
    //         repetitionRange,
    //         2,
    //         muscleGroups,
    //         advanceTrainingTechniques
    //     );
    //     TemplateSet set2 = TemplateSet.Create
    //     (
    //         exercise2.Id,
    //         repetitionRange,
    //         2,
    //         muscleGroups,
    //         advanceTrainingTechniques
    //     );
    //     TemplateSet set3 = TemplateSet.Create
    //     (
    //         exercise3.Id,
    //         repetitionRange,
    //         2,
    //         muscleGroups,
    //         advanceTrainingTechniques
    //     );
    //     GetSetsFromSessionTemplateQuery query = new GetSetsFromSessionTemplateQuery
    //     (
    //         templateSession.Id,
    //         trainingTemplate.Id
    //     );
    //     GetSetsFromSessionTemplateQueryHandler queryHandler =
    //     new GetSetsFromSessionTemplateQueryHandler(_trainingTemplateRepository);
    //     // act
    //     var result = await queryHandler.HandleAsync(query,default);
    //     // assert
        
    // }
}