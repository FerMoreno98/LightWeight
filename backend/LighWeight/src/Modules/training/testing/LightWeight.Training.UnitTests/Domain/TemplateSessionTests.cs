using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class TemplateSessionTests
{
    [Theory]
    [InlineData("Valid Name")]
    [InlineData("123456")]
    [InlineData("__?")]
    public void Create_WithValidDate_ReturnsSessionTemplate
    (
        string name
    )
    {   
        // Arrange
        // Act
        TemplateSession session = TemplateSession.Create
        (
            name
        );
        // Assert
        Assert.Equal(name,session.Name);

    }
    [Theory]
    [InlineData("")]
    public void Create_WithInvalidDate_ThrowsDomainException
    (
        string name
    )
    {
        // Arrange
        // Act

        // Assert
        Assert.Throws<SessionNameEmptyDomainException>(
        () => TemplateSession.Create
        (
            name
        )
        );
        
    }
    [Fact]
    public void GetNumberOfSeriesPerGroupPerSession_WithTwoSetsOfTheSameMuscleGroup_SumBoth()
    {
        // Arrange
        List<MuscleGroups> muscleGroups = new List<MuscleGroups>();
        muscleGroups.Add(MuscleGroups.Biceps);
        RepetitionRange repetitionRange = RepetitionRange.Create(10,12);
        AdvanceTrainingTechniques advanceTrainingTechniques = AdvanceTrainingTechniques.Create(false,false,false);
        Exercise exercise = Exercise.Create
        (
            "EjercicioPrueba",
            true,
            muscleGroups
        );
        TemplateSession session = TemplateSession.Create
        (
            "ValidName"
        );
        TemplateSet templateSet = TemplateSet.Create
        (
            exercise.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        TemplateSet templateSet2 = TemplateSet.Create
        (
            exercise.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        session.AddSet(templateSet);
        session.AddSet(templateSet2);

        // Act
        Dictionary<MuscleGroups,int> ret = session.GetNumberOfSeriesPerGroupPerSession();
        // Assert
        Assert.NotEmpty(ret);
        Assert.Single(ret);
        Assert.Equal(2,ret[MuscleGroups.Biceps]);
    }

    [Fact]
    public void GetNumberOfSeriesPerGroupPerSession_WithTwoSetsOfDifferentMuscleGroup_RepresentsBothOnTheDict()
    {
        // Arrange
        List<MuscleGroups> muscleGroups = new List<MuscleGroups>();
        muscleGroups.Add(MuscleGroups.Biceps);
        List<MuscleGroups> muscleGroups2 = new List<MuscleGroups>();
        muscleGroups2.Add(MuscleGroups.Back);

        RepetitionRange repetitionRange = RepetitionRange.Create(10,12);
        AdvanceTrainingTechniques advanceTrainingTechniques = AdvanceTrainingTechniques.Create(false,false,false);
        Exercise exercise = Exercise.Create
        (
            "EjercicioPrueba",
            true,
            muscleGroups
        );
        Exercise exercise2 = Exercise.Create
        (
            "EjercicioPrueba2",
            true,
            muscleGroups2
        );
        TemplateSession session = TemplateSession.Create
        (
            "ValidName"
        );
        TemplateSet templateSet = TemplateSet.Create
        (
            exercise.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        TemplateSet templateSet2 = TemplateSet.Create
        (
            exercise2.Id,
            repetitionRange,
            2,
            muscleGroups2,
            advanceTrainingTechniques
        );
        session.AddSet(templateSet);
        session.AddSet(templateSet2);

        // Act
        Dictionary<MuscleGroups,int> ret = session.GetNumberOfSeriesPerGroupPerSession();
        // Assert
        Assert.NotEmpty(ret);
        Assert.Equal(1,ret[MuscleGroups.Biceps]);
        Assert.Equal(1,ret[MuscleGroups.Back]);
    }
    [Fact]
    public void GetNumberOfSeriesPerGroupPerSession_WithNoSets_ReturnsEmptyDict()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create
        (
            "ValidName"
        );
        // Act
        Dictionary<MuscleGroups,int> ret = session.GetNumberOfSeriesPerGroupPerSession();
        // Assert
        Assert.Empty(ret);
        
    }


}