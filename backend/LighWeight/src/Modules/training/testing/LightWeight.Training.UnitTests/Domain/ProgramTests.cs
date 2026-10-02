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
            VolumeLandmarks.MEV,
            TrainingDistribution.FullBody,
            7,
            1
        );
        // Act
        program.AddTrainingTemplate(template);
        // Assert
        Assert.Single(program.trainingTemplates);
        Assert.Contains(template, program.trainingTemplates);
    }
}
