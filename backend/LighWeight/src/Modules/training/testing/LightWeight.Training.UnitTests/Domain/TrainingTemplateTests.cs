using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class TrainingTemplateTests
{
    [Theory]
    [InlineData("NombreValido", VolumeLandmarks.MV, TrainingDistribution.FullBody)]
    [InlineData("123435",VolumeLandmarks.MAV,TrainingDistribution.PushPullLegs)]
    [InlineData("A_23",VolumeLandmarks.MEV,TrainingDistribution.Phat)]
    public void Create_WithValidData_ReturnATrainingTemplate
    (
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution
    )
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        TrainingTemplate template = TrainingTemplate.Create
        (
            userId,
            name,
            volumeLandmark,
            distribution
        );
        // Act
        // Assert
        Assert.Equal(userId,template.UserId);
        Assert.Equal(name,template.Name);
        Assert.Equal(volumeLandmark,template.VolumeLandmark);
        Assert.Equal(distribution,template.TrainingDistribution);
    }

    [Theory]
    [InlineData("",VolumeLandmarks.MEV,TrainingDistribution.Phat)]
    public void Create_WithEmptyName_ThrowsDomainException
    (
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution
    )
    {
                // Arrange
        var userId = Guid.CreateVersion7();

        // Act
        // Assert
        Assert.Throws<NameEmptyDomainException>(
        ()=> TrainingTemplate.Create
        (
            userId,
            name,
            volumeLandmark,
            distribution
        )
        );
        
    }
    [Theory]
    [InlineData("NombreValido",VolumeLandmarks.MEV,TrainingDistribution.Phat)]
    public void Create_WithEmptyGuid_ThrowsDomainException
    (
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution
    )
    {
        // Arrange
        var userId = Guid.Empty;

        // Act
        // Assert
        Assert.Throws<UserIdEmptyDomainException>(
        ()=> TrainingTemplate.Create
        (
            userId,
            name,
            volumeLandmark,
            distribution
        )
        );
    }

    [Theory]
    [InlineData("ValidName",(VolumeLandmarks)999,TrainingDistribution.Phat)]
    public void Create_WithInvalidLandmark_ThrowsDomainException
    (
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution
    )
    {
        // Arrange
        var userId = Guid.CreateVersion7();

        // Act
        // Assert
        Assert.Throws<InvalidVolumeLandmarkDomainException>(
        ()=> TrainingTemplate.Create
        (
            userId,
            name,
            volumeLandmark,
            distribution
        )
        );
    }

    [Theory]
    [InlineData("ValidName",VolumeLandmarks.MEV,(TrainingDistribution)999)]
    public void Create_WithInvalidTrainingDistribution_ThrowsDomainException
    (
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution distribution
    )
    {
        // Arrange
        var userId = Guid.CreateVersion7();

        // Act
        // Assert
        Assert.Throws<InvalidTrainingDistributionDomainException>(
        ()=> TrainingTemplate.Create
        (
            userId,
            name,
            volumeLandmark,
            distribution
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
            Guid.CreateVersion7(),
            "ValidName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat
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
            Guid.CreateVersion7(),
            "ValidName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat
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
            Guid.CreateVersion7(),
            "ValidName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat
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
            Guid.CreateVersion7(),
            "ValidName",
            VolumeLandmarks.MEV,
            TrainingDistribution.Phat
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