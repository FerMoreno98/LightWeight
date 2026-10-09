using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class TrainingTemplateTests
{
    [Theory]
    [InlineData("Hipertrofia base", VolumeLandmarks.MV, TrainingDistribution.FullBody, 7)]
    [InlineData("Deload", VolumeLandmarks.MAV, TrainingDistribution.PushPullLegs, 5)]
    [InlineData("Bloque 3", VolumeLandmarks.MEV, TrainingDistribution.Phat, 10)]
    public void Create_WithValidData_ReturnATrainingTemplate
    (
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution,
        int durationInDays
    )
    {
        // Arrange
        // Act
        TrainingTemplate template = TrainingTemplate.Create
        (
            name,
            volumeLandmark,
            distribution,
            durationInDays
        );
        // Assert
        Assert.Equal(name,template.Name);
        Assert.Equal(volumeLandmark,template.VolumeLandmark);
        Assert.Equal(distribution,template.TrainingDistribution);
        Assert.Equal(durationInDays,template.DurationInDays);
        Assert.False(template.IsDeleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<NameEmptyDomainException>(
        ()=> TrainingTemplate.Create
        (
            name!,
            VolumeLandmarks.MEV,
            TrainingDistribution.FullBody,
            7
        )
        );
    }

    [Fact]
    public void Create_TrimsTheName()
    {
        // Arrange
        // Act
        TrainingTemplate template = TrainingTemplate.Create("  Deload  ", VolumeLandmarks.MV, TrainingDistribution.FullBody, 7);
        // Assert
        Assert.Equal("Deload", template.Name);
    }

    [Fact]
    public void Rename_WithValidName_ChangesTheName()
    {
        // Arrange
        TrainingTemplate template = TrainingTemplate.Create("Old name", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        // Act
        template.Rename(" New name ");
        // Assert
        Assert.Equal("New name", template.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyName_ThrowsDomainExceptionAndKeepsTheName(string name)
    {
        // Arrange
        TrainingTemplate template = TrainingTemplate.Create("Old name", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        // Act
        // Assert
        Assert.Throws<NameEmptyDomainException>(() => template.Rename(name));
        Assert.Equal("Old name", template.Name);
    }

    [Theory]
    [InlineData((VolumeLandmarks)999,TrainingDistribution.Phat)]
    public void Create_WithInvalidLandmark_ThrowsDomainException
    (
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution
    )
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<InvalidVolumeLandmarkDomainException>(
        ()=> TrainingTemplate.Create
        (
            "ValidTemplateName",
            volumeLandmark,
            distribution,
            7
        )
        );
    }

    [Theory]
    [InlineData(VolumeLandmarks.MEV,(TrainingDistribution)999)]
    public void Create_WithInvalidTrainingDistribution_ThrowsDomainException
    (
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution
    )
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<InvalidTrainingDistributionDomainException>(
        ()=> TrainingTemplate.Create
        (
            "ValidTemplateName",
            volumeLandmark,
            distribution,
            7
        )
        );
    }

    [Fact]
    public void GetNumberOfSeriesPerGroup_WithTwoSetsOfTheSameMuscleGroup_SumTheTwoSets()
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
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            "ValidTemplateName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7
        );
        TemplateSession templateSession = TemplateSession.Create
        (
            "Session1"
        );
        TemplateSet templateSet = TemplateSet.Create
        (
            exercise.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        templateSession.AddSet(templateSet);
        trainingTemplate.AddSessionTemplate(templateSession);
        TemplateSession templateSession2 = TemplateSession.Create
        (
            "Session2"
        );
        TemplateSet templateSet2 = TemplateSet.Create
        (
            exercise.Id,
            repetitionRange,
            2,
            muscleGroups,
            advanceTrainingTechniques
        );
        templateSession2.AddSet(templateSet2);
        trainingTemplate.AddSessionTemplate(templateSession2);
        // Act
        Dictionary<MuscleGroups,int> ret = trainingTemplate.GetNumberOfSeriesPerGroup();
        // Assert
        Assert.NotEmpty(ret);
        Assert.Single(ret);
        Assert.Equal(2,ret[MuscleGroups.Biceps]);
    }

    [Fact]
    public void GetNumberOfSeriesPerGroup_WithTwoSetsOfTheSameMuscleGroupInTheSameSession_SumTheTwoSets()
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
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            "ValidTemplateName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7
        );
        TemplateSession templateSession = TemplateSession.Create
        (
            "Session1"
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
        templateSession.AddSet(templateSet2);
        templateSession.AddSet(templateSet);
        trainingTemplate.AddSessionTemplate(templateSession);

  
        // Act
        Dictionary<MuscleGroups,int> ret = trainingTemplate.GetNumberOfSeriesPerGroup();
        // Assert
        Assert.NotEmpty(ret);
        Assert.Single(ret);
        Assert.Equal(2,ret[MuscleGroups.Biceps]);
    }
    [Fact]
    public void GetNumberOfSeriesPerGroup_EmptyTrainingTemplate_ShouldReturnAnEmptyDictionary()
    {
        // Arrange
        TrainingTemplate template = TrainingTemplate.Create
        (
            "ValidTemplateName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7
        );

        Dictionary<MuscleGroups,int> ret = template.GetNumberOfSeriesPerGroup();
        // Assert
        Assert.Empty(ret);
    }

    [Fact]
    public void GetNumberOfSeriesPerGroup_EmptySessionTemplate_ShouldReturnAnEmptyDictionary()
    {
        // Arrange
        TrainingTemplate template = TrainingTemplate.Create
        (
            "ValidTemplateName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7
        );
        TemplateSession templateSession = TemplateSession.Create
        (
            "Session1"
        );
        template.AddSessionTemplate(templateSession);
        // Act
        Dictionary<MuscleGroups,int> ret = template.GetNumberOfSeriesPerGroup();
        // Assert
        Assert.Empty(ret);
    }
}