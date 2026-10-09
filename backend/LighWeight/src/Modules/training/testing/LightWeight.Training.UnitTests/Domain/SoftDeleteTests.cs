using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class SoftDeleteTests
{
    private static readonly DateTime Now = new(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc);

    private static TemplateSet CreateTemplateSet(MuscleGroups muscleGroup = MuscleGroups.Biceps)
    {
        return TemplateSet.Create
        (
            Guid.CreateVersion7(),
            RepetitionRange.Create(12, 10),
            2,
            new List<MuscleGroups> { muscleGroup },
            AdvanceTrainingTechniques.None
        );
    }

    private static TrainingTemplate CreateTrainingTemplate()
    {
        return TrainingTemplate.Create
        (
            "ValidTemplateName",
            VolumeLandmarks.MEV,
            TrainingDistribution.FullBody,
            7
        );
    }

    [Fact]
    public void DeleteTemplateSet_WithExistingSet_MarksItAsDeletedAndHidesIt()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Session1");
        TemplateSet set = CreateTemplateSet();
        session.AddSet(set);
        // Act
        session.DeleteTemplateSet(set.Id, Now);
        // Assert
        Assert.True(set.IsDeleted);
        Assert.Equal(Now, set.DeletedAt);
        Assert.Empty(session.TemplateExercises);
    }

    [Fact]
    public void DeleteTemplateSet_WithAlreadyDeletedSet_ThrowsDomainException()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Session1");
        TemplateSet set = CreateTemplateSet();
        session.AddSet(set);
        session.DeleteTemplateSet(set.Id, Now);
        // Act
        // Assert
        Assert.Throws<SetNotFoundDomainException>(() => session.DeleteTemplateSet(set.Id, Now));
    }

    [Fact]
    public void GetNumberOfSeriesPerGroupPerSession_WithDeletedSet_IgnoresIt()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Session1");
        TemplateSet bicepsSet = CreateTemplateSet(MuscleGroups.Biceps);
        TemplateSet backSet = CreateTemplateSet(MuscleGroups.Back);
        session.AddSet(bicepsSet);
        session.AddSet(backSet);
        session.DeleteTemplateSet(bicepsSet.Id, Now);
        // Act
        Dictionary<MuscleGroups,int> ret = session.GetNumberOfSeriesPerGroupPerSession();
        // Assert
        Assert.Single(ret);
        Assert.Equal(1, ret[MuscleGroups.Back]);
    }

    [Fact]
    public void DeleteSessionTemplate_WithExistingSession_MarksSessionAndItsSetsAsDeleted()
    {
        // Arrange
        TrainingTemplate template = CreateTrainingTemplate();
        TemplateSession session = TemplateSession.Create("Session1");
        TemplateSet set = CreateTemplateSet();
        session.AddSet(set);
        template.AddSessionTemplate(session);
        // Act
        template.DeleteSessionTemplate(session.Id, Now);
        // Assert
        Assert.True(session.IsDeleted);
        Assert.Equal(Now, session.DeletedAt);
        Assert.True(set.IsDeleted);
        Assert.Empty(template.TemplateSessions);
        Assert.Empty(template.GetNumberOfSeriesPerGroup());
    }

    [Fact]
    public void DeleteSessionTemplate_WithAlreadyDeletedSession_ThrowsDomainException()
    {
        // Arrange
        TrainingTemplate template = CreateTrainingTemplate();
        TemplateSession session = TemplateSession.Create("Session1");
        template.AddSessionTemplate(session);
        template.DeleteSessionTemplate(session.Id, Now);
        // Act
        // Assert
        Assert.Throws<SessionNotFoundDomainException>(() => template.DeleteSessionTemplate(session.Id, Now));
    }

    [Fact]
    public void DeleteTrainingTemplate_WithExistingTemplate_MarksWholeHierarchyAsDeleted()
    {
        // Arrange
        Program program = Program.Create
        (
            Guid.CreateVersion7(),
            Periodization.Linear,
            "Program1",
            new List<MuscleGroups> { MuscleGroups.Biceps }
        );
        TrainingTemplate template = CreateTrainingTemplate();
        TemplateSession session = TemplateSession.Create("Session1");
        TemplateSet set = CreateTemplateSet();
        session.AddSet(set);
        template.AddSessionTemplate(session);
        program.AddTrainingTemplate(template);
        // Act
        program.DeleteTrainingTemplate(template.Id, Now);
        // Assert
        Assert.True(template.IsDeleted);
        Assert.Equal(Now, template.DeletedAt);
        Assert.True(session.IsDeleted);
        Assert.True(set.IsDeleted);
        Assert.Empty(program.trainingTemplates);
    }

    [Fact]
    public void DeleteTrainingTemplate_WithUnknownTemplate_ThrowsDomainException()
    {
        // Arrange
        Program program = Program.Create
        (
            Guid.CreateVersion7(),
            Periodization.Linear,
            "Program1",
            new List<MuscleGroups> { MuscleGroups.Biceps }
        );
        // Act
        // Assert
        Assert.Throws<TrainingTemplateNotFoundDomainException>(
            () => program.DeleteTrainingTemplate(Guid.CreateVersion7(), Now));
    }
}
