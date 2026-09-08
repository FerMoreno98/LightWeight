using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Application.Queries.TrainingTemplates.GetUserTrainingTemplates;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class TrainingTemplateTests
{
    [Theory]
    [InlineData("ValidName","MV","FullBody")]
    [InlineData("1234","MAV","UpperLower")]
    [InlineData("Valid__?Name","MV","FullBody")]
    public async Task CreateTrainingTemplateCommand_WithValidData_ReturnsGuid
    (
        string Name,
        string VolumeLandmark,
        string TrainingDistribution
    )
    {
        // Arrange
        ITrainingTemplateRepository _trainingTemplate = Substitute.For<ITrainingTemplateRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        CreateTrainingTemplateCommandHandler commandHandler = new CreateTrainingTemplateCommandHandler
        (
            _trainingTemplate,
            _Uow
        );
        Guid UserId = Guid.CreateVersion7();
        CreateTrainingTemplateCommand command = new CreateTrainingTemplateCommand
        (
            UserId,
            Name,
            VolumeLandmark,
            TrainingDistribution
        );
        // Act
        Guid TemplateId = await commandHandler.HandleAsync(command,default);
        // Assert
        Assert.NotEqual(Guid.Empty,TemplateId);
    }

    [Fact]
    public async Task GetUserTrainingTemplate_ReturnsDataSuccessfullyAsync()
    {
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
        List<TrainingTemplate> templates = new List<TrainingTemplate>();
        templates.Add(trainingTemplate);
        _trainingTemplateRepository
            .GetAllTrainingTemplatesOfAUserAsync(userId)
                .Returns(templates);
        GetUserTrainingTemplatesQuery query = new GetUserTrainingTemplatesQuery
        (
            userId
        );
        GetUserTrainingTemplatesQueryHandler queryHandler =
        new GetUserTrainingTemplatesQueryHandler(_trainingTemplateRepository);
        // act
        var result = await queryHandler.HandleAsync(query,default);
        // assert
        Assert.All(result, r => Assert.Equal(6, r.TotalVolume["Biceps"]));
        Assert.All(result, r => Assert.Equal(3, r.TotalVolume["Back"]));
        Assert.All(result, r => Assert.Equal(3, r.TotalVolume["Chest"]));
        
    
    }
}