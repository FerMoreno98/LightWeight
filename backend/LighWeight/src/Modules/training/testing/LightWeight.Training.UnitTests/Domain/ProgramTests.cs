using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.UnitTests.Domain;

public class ProgramTests
{
    [Theory]
    [InlineData("NombreValido", Periodization.Linear)]
    [InlineData("123435", Periodization.Ondulating)]
    [InlineData("A_23", Periodization.MikeIsraetel)]
    public void Create_WithValidData_ReturnsAProgram
    (
        string name,
        Periodization periodization
    )
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        List<MuscleGroups> muscleGroups = new List<MuscleGroups> { MuscleGroups.Back, MuscleGroups.Chest };
        // Act
        Program program = Program.Create
        (
            userId,
            periodization,
            name,
            muscleGroups
        );
        // Assert
        Assert.Equal(userId, program.UserId);
        Assert.Equal(name, program.Name);
        Assert.Equal(periodization, program.Periodization);
        Assert.Equal(muscleGroups, program.AimMuscleGroups);
        Assert.Empty(program.trainingTemplates);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsDomainException(string name)
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        // Act
        // Assert
        Assert.Throws<NameEmptyDomainException>(
        () => Program.Create
        (
            userId,
            Periodization.Linear,
            name,
            new List<MuscleGroups>()
        )
        );
    }

    [Fact]
    public void Create_WithEmptyGuid_ThrowsDomainException()
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<UserIdEmptyDomainException>(
        () => Program.Create
        (
            Guid.Empty,
            Periodization.Linear,
            "ValidName",
            new List<MuscleGroups>()
        )
        );
    }

    [Fact]
    public void AddTrainingTemplate_AddsTheTemplateToTheProgram()
    {
        // Arrange
        Program program = Program.Create
        (
            Guid.CreateVersion7(),
            Periodization.Linear,
            "ValidName",
            new List<MuscleGroups>()
        );
        TrainingTemplate template = TrainingTemplate.Create
        (
            "ValidTemplateName",
            VolumeLandmarks.MEV,
            TrainingDistribution.FullBody,
            7
        );
        // Act
        program.AddTrainingTemplate(template);
        // Assert
        Assert.Single(program.trainingTemplates);
        Assert.Contains(template, program.trainingTemplates);
    }

    private static Program CreateValidProgram()
    {
        return Program.Create(Guid.CreateVersion7(), Periodization.Linear, "ValidProgramName", new List<MuscleGroups>());
    }

    private static TrainingTemplate CreateValidTemplate(string name = "ValidTemplateName")
    {
        return TrainingTemplate.Create(name, VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
    }

    [Fact]
    public void AddTrainingTemplate_AssignsConsecutiveOrders()
    {
        // Arrange
        Program program = CreateValidProgram();
        TrainingTemplate first = CreateValidTemplate();
        TrainingTemplate second = CreateValidTemplate();
        TrainingTemplate third = CreateValidTemplate();
        // Act
        program.AddTrainingTemplate(first);
        program.AddTrainingTemplate(second);
        program.AddTrainingTemplate(third);
        // Assert
        Assert.Equal(1, first.Order);
        Assert.Equal(2, second.Order);
        Assert.Equal(3, third.Order);
    }

    [Fact]
    public void AddTrainingTemplate_AfterDeletingOne_NeverRepeatsTheOrderOfAnActiveTemplate()
    {
        // Arrange
        Program program = CreateValidProgram();
        TrainingTemplate first = CreateValidTemplate();
        TrainingTemplate second = CreateValidTemplate();
        TrainingTemplate third = CreateValidTemplate();
        program.AddTrainingTemplate(first);
        program.AddTrainingTemplate(second);
        program.AddTrainingTemplate(third);
        program.DeleteTrainingTemplate(second.Id, DateTime.UtcNow);
        TrainingTemplate fourth = CreateValidTemplate();
        // Act
        program.AddTrainingTemplate(fourth);
        // Assert
        Assert.Equal(4, fourth.Order);
        Assert.Equal(program.trainingTemplates.Count, program.trainingTemplates.Select(t => t.Order).Distinct().Count());
    }

    [Fact]
    public void RenameTrainingTemplate_WithExistingTemplate_ChangesItsName()
    {
        // Arrange
        Program program = CreateValidProgram();
        TrainingTemplate template = CreateValidTemplate("Old name");
        program.AddTrainingTemplate(template);
        // Act
        program.RenameTrainingTemplate(template.Id, "New name");
        // Assert
        Assert.Equal("New name", template.Name);
    }

    [Fact]
    public void RenameTrainingTemplate_WithNonExistingTemplate_ThrowsDomainException()
    {
        // Arrange
        Program program = CreateValidProgram();
        // Act
        // Assert
        Assert.Throws<TrainingTemplateNotFoundDomainException>
        (
            () => program.RenameTrainingTemplate(Guid.CreateVersion7(), "New name")
        );
    }

    [Fact]
    public void RenameTrainingTemplate_WithSoftDeletedTemplate_ThrowsDomainException()
    {
        // Arrange
        Program program = CreateValidProgram();
        TrainingTemplate template = CreateValidTemplate("Old name");
        program.AddTrainingTemplate(template);
        program.DeleteTrainingTemplate(template.Id, DateTime.UtcNow);
        // Act
        // Assert
        Assert.Throws<TrainingTemplateNotFoundDomainException>
        (
            () => program.RenameTrainingTemplate(template.Id, "New name")
        );
        Assert.Equal("Old name", template.Name);
    }
}
