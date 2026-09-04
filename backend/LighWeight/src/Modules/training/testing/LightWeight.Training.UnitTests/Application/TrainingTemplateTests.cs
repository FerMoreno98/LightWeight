using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
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
}