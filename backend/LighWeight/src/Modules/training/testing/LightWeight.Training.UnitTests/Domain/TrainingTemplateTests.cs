using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class TrainingTemplateTests
{
    [Theory]
    [InlineData(VolumeLandmarks.MV, TrainingDistribution.FullBody, 7, 1)]
    [InlineData(VolumeLandmarks.MAV, TrainingDistribution.PushPullLegs, 5, 2)]
    [InlineData(VolumeLandmarks.MEV, TrainingDistribution.Phat, 10, 3)]
    public void Create_WithValidData_ReturnATrainingTemplate
    (
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution,
        int durationInDays,
        int order
    )
    {
        // Arrange
        // Act
        TrainingTemplate template = TrainingTemplate.Create
        (
            volumeLandmark,
            distribution,
            durationInDays,
            order
        );
        // Assert
        Assert.Equal(volumeLandmark,template.VolumeLandmark);
        Assert.Equal(distribution,template.TrainingDistribution);
        Assert.Equal(durationInDays,template.DurationInDays);
        Assert.Equal(order,template.Order);
        Assert.False(template.IsDeleted);
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
            volumeLandmark,
            distribution,
            7,
            1
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
            volumeLandmark,
            distribution,
            7,
            1
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
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7,
            1
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
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7,
            1
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
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7,
            1
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
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat,
            7,
            1
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