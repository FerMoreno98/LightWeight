using System.Buffers.Text;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class ProgramSharingTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private static Program CreateProgram(Guid? userId = null)
    {
        return Program.Create
        (
            userId ?? Guid.CreateVersion7(),
            Periodization.Linear,
            "Push Pull Legs",
            new List<MuscleGroups> { MuscleGroups.Chest, MuscleGroups.Back }
        );
    }

    private static TemplateSet CreateTemplateSet(Guid? superSetGroupId = null)
    {
        return TemplateSet.Create
        (
            Guid.CreateVersion7(),
            RepetitionRange.Create(6, 10),
            8,
            new List<MuscleGroups> { MuscleGroups.Chest },
            AdvanceTrainingTechniques.Create(true, false, false),
            superSetGroupId
        );
    }

    // ---------------------------------------------------------------- Share / RevokeShare

    [Fact]
    public void Share_GeneratesAUrlSafeTokenOfTheExpectedLength()
    {
        // Arrange
        Program program = CreateProgram();
        // Act
        string token = program.Share();
        // Assert
        Assert.Equal(Program.ShareTokenLength, token.Length);
        Assert.True(Base64Url.IsValid(token));
        Assert.Equal(token, program.ShareToken);
    }

    [Fact]
    public void Share_WhenAlreadyShared_ReturnsTheSameToken()
    {
        // Arrange
        Program program = CreateProgram();
        string first = program.Share();
        // Act
        string second = program.Share();
        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Share_DifferentPrograms_GetDifferentTokens()
    {
        // Act
        string first = CreateProgram().Share();
        string second = CreateProgram().Share();
        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void RevokeShare_RemovesTheToken()
    {
        // Arrange
        Program program = CreateProgram();
        program.Share();
        // Act
        program.RevokeShare();
        // Assert
        Assert.Null(program.ShareToken);
    }

    [Fact]
    public void Share_AfterRevoking_GeneratesANewToken()
    {
        // Arrange
        Program program = CreateProgram();
        string revoked = program.Share();
        program.RevokeShare();
        // Act
        string token = program.Share();
        // Assert
        Assert.NotEqual(revoked, token);
    }

    // ---------------------------------------------------------------- CopyFor

    [Fact]
    public void CopyFor_CreatesAProgramOfTheOtherUserWithTheSameDataAndItsOrigin()
    {
        // Arrange
        Guid authorId = Guid.CreateVersion7();
        Guid receiverId = Guid.CreateVersion7();
        Program original = CreateProgram(authorId);
        // Act
        Program copy = original.CopyFor(receiverId);
        // Assert
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(receiverId, copy.UserId);
        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.Periodization, copy.Periodization);
        Assert.Equal(original.AimMuscleGroups, copy.AimMuscleGroups);
        Assert.NotSame(original.AimMuscleGroups, copy.AimMuscleGroups);
        Assert.Equal(original.Id, copy.CopiedFromProgramId);
        Assert.Equal(authorId, copy.CopiedFromUserId);
    }

    [Fact]
    public void CopyFor_DoesNotCopyTheShareToken()
    {
        // Arrange
        Program original = CreateProgram();
        original.Share();
        // Act
        Program copy = original.CopyFor(Guid.CreateVersion7());
        // Assert
        Assert.Null(copy.ShareToken);
        Assert.NotNull(original.ShareToken);
    }

    [Fact]
    public void CopyFor_CopiesTheWholeHierarchyKeepingTheNamesWithNewIds()
    {
        // Arrange
        Program original = CreateProgram();
        TrainingTemplate template = TrainingTemplate.Create("Hipertrofia base", VolumeLandmarks.MAV, TrainingDistribution.UpperLower, 7);
        TemplateSession session = TemplateSession.Create("Upper A");
        TemplateSet set = CreateTemplateSet();
        session.AddSet(set);
        template.AddSessionTemplate(session);
        original.AddTrainingTemplate(template);
        // Act
        Program copy = original.CopyFor(Guid.CreateVersion7());
        // Assert
        TrainingTemplate copiedTemplate = Assert.Single(copy.trainingTemplates);
        Assert.NotEqual(template.Id, copiedTemplate.Id);
        Assert.Equal("Hipertrofia base", copiedTemplate.Name);
        Assert.Equal(template.VolumeLandmark, copiedTemplate.VolumeLandmark);
        Assert.Equal(template.TrainingDistribution, copiedTemplate.TrainingDistribution);
        Assert.Equal(template.DurationInDays, copiedTemplate.DurationInDays);

        TemplateSession copiedSession = Assert.Single(copiedTemplate.TemplateSessions);
        Assert.NotEqual(session.Id, copiedSession.Id);
        Assert.Equal("Upper A", copiedSession.Name);

        TemplateSet copiedSet = Assert.Single(copiedSession.TemplateExercises);
        Assert.NotEqual(set.Id, copiedSet.Id);
        Assert.Equal(set.ExerciseId, copiedSet.ExerciseId);
        Assert.Equal(set.RepetitionRange, copiedSet.RepetitionRange);
        Assert.Equal(set.ExpectedRPE, copiedSet.ExpectedRPE);
        Assert.Equal(set.AdvanceTrainingTechniques, copiedSet.AdvanceTrainingTechniques);
        Assert.Equal(set.AimMuscleGroups, copiedSet.AimMuscleGroups);
    }

    [Fact]
    public void CopyFor_KeepsTheOrderOfTheTemplates()
    {
        // Arrange
        Program original = CreateProgram();
        original.AddTrainingTemplate(TrainingTemplate.Create("Primera", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7));
        original.AddTrainingTemplate(TrainingTemplate.Create("Segunda", VolumeLandmarks.MAV, TrainingDistribution.FullBody, 7));
        original.AddTrainingTemplate(TrainingTemplate.Create("Tercera", VolumeLandmarks.MRV, TrainingDistribution.FullBody, 7));
        // Act
        Program copy = original.CopyFor(Guid.CreateVersion7());
        // Assert
        List<string> names = copy.trainingTemplates.OrderBy(t => t.Order).Select(t => t.Name).ToList();
        Assert.Equal(new[] { "Primera", "Segunda", "Tercera" }, names);
        Assert.Equal(new[] { 1, 2, 3 }, copy.trainingTemplates.OrderBy(t => t.Order).Select(t => t.Order));
    }

    [Fact]
    public void CopyFor_SkipsSoftDeletedTemplatesSessionsAndSets()
    {
        // Arrange
        Program original = CreateProgram();
        TrainingTemplate activeTemplate = TrainingTemplate.Create("Activa", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        TrainingTemplate deletedTemplate = TrainingTemplate.Create("Borrada", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        TemplateSession activeSession = TemplateSession.Create("Active");
        TemplateSession deletedSession = TemplateSession.Create("Deleted");
        TemplateSet activeSet = CreateTemplateSet();
        TemplateSet deletedSet = CreateTemplateSet();
        activeSession.AddSet(activeSet);
        activeSession.AddSet(deletedSet);
        activeTemplate.AddSessionTemplate(activeSession);
        activeTemplate.AddSessionTemplate(deletedSession);
        original.AddTrainingTemplate(activeTemplate);
        original.AddTrainingTemplate(deletedTemplate);
        activeSession.DeleteTemplateSet(deletedSet.Id, Now);
        activeTemplate.DeleteSessionTemplate(deletedSession.Id, Now);
        original.DeleteTrainingTemplate(deletedTemplate.Id, Now);
        // Act
        Program copy = original.CopyFor(Guid.CreateVersion7());
        // Assert
        TrainingTemplate copiedTemplate = Assert.Single(copy.trainingTemplates);
        Assert.Equal("Activa", copiedTemplate.Name);
        TemplateSession copiedSession = Assert.Single(copiedTemplate.TemplateSessions);
        Assert.Equal("Active", copiedSession.Name);
        Assert.Single(copiedSession.TemplateExercises);
    }

    [Fact]
    public void CopyFor_GivesNewSuperSetGroupIdsAndKeepsTheSetsGrouped()
    {
        // Arrange
        Program original = CreateProgram();
        TrainingTemplate template = TrainingTemplate.Create("Plantilla", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        TemplateSession session = TemplateSession.Create("Session");
        Guid superSet = Guid.CreateVersion7();
        session.AddSet(CreateTemplateSet(superSet));
        session.AddSet(CreateTemplateSet(superSet));
        session.AddSet(CreateTemplateSet());
        template.AddSessionTemplate(session);
        original.AddTrainingTemplate(template);
        // Act
        Program copy = original.CopyFor(Guid.CreateVersion7());
        // Assert
        List<TemplateSet> copiedSets = Assert.Single(Assert.Single(copy.trainingTemplates).TemplateSessions).TemplateExercises.ToList();
        Assert.NotNull(copiedSets[0].SuperSetGroupId);
        Assert.NotEqual(superSet, copiedSets[0].SuperSetGroupId);
        Assert.Equal(copiedSets[0].SuperSetGroupId, copiedSets[1].SuperSetGroupId);
        Assert.Null(copiedSets[2].SuperSetGroupId);
    }

    [Fact]
    public void CopyFor_LeavesTheOriginalUnchanged()
    {
        // Arrange
        Program original = CreateProgram();
        TrainingTemplate template = TrainingTemplate.Create("Plantilla", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        original.AddTrainingTemplate(template);
        // Act
        original.CopyFor(Guid.CreateVersion7());
        // Assert
        Assert.Same(template, Assert.Single(original.trainingTemplates));
        Assert.Null(original.CopiedFromProgramId);
        Assert.Null(original.CopiedFromUserId);
    }

    [Fact]
    public void DuplicateTrainingTemplate_StillAddsTheCopySuffix()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate template = TrainingTemplate.Create("Plantilla", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        program.AddTrainingTemplate(template);
        // Act
        TrainingTemplate copy = program.DuplicateTrainingTemplate(template.Id);
        // Assert
        Assert.Equal("Plantilla" + TrainingTemplate.CopySuffix, copy.Name);
    }
}
