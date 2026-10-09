using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class DuplicateTrainingTemplateTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private static Program CreateProgram()
    {
        return Program.Create
        (
            Guid.CreateVersion7(),
            Periodization.Linear,
            "ValidProgramName",
            new List<MuscleGroups>()
        );
    }

    private static TemplateSet CreateTemplateSet(Guid? superSetGroupId = null, MuscleGroups muscleGroup = MuscleGroups.Biceps)
    {
        return TemplateSet.Create
        (
            Guid.CreateVersion7(),
            RepetitionRange.Create(8, 12),
            8,
            new List<MuscleGroups> { muscleGroup },
            AdvanceTrainingTechniques.None,
            superSetGroupId
        );
    }

    [Fact]
    public void DuplicateTrainingTemplate_CopiesTheWholeHierarchyWithNewIds()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate original = TrainingTemplate.Create(VolumeLandmarks.MAV, TrainingDistribution.UpperLower, 7, 1);
        TemplateSession session = TemplateSession.Create("Upper A");
        TemplateSet set = TemplateSet.Create
        (
            Guid.CreateVersion7(),
            RepetitionRange.Create(6, 10),
            9,
            new List<MuscleGroups> { MuscleGroups.Chest, MuscleGroups.Triceps },
            AdvanceTrainingTechniques.Create(true, false, false)
        );
        session.AddSet(set);
        original.AddSessionTemplate(session);
        program.AddTrainingTemplate(original);
        // Act
        TrainingTemplate copy = program.DuplicateTrainingTemplate(original.Id);
        // Assert
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(original.VolumeLandmark, copy.VolumeLandmark);
        Assert.Equal(original.TrainingDistribution, copy.TrainingDistribution);
        Assert.Equal(original.DurationInDays, copy.DurationInDays);
        Assert.Contains(copy, program.trainingTemplates);

        TemplateSession copiedSession = Assert.Single(copy.TemplateSessions);
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
    public void DuplicateTrainingTemplate_LeavesTheOriginalUnchanged()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate original = TrainingTemplate.Create(VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7, 1);
        TemplateSession session = TemplateSession.Create("FullBody A");
        TemplateSet set = CreateTemplateSet();
        session.AddSet(set);
        original.AddSessionTemplate(session);
        program.AddTrainingTemplate(original);
        // Act
        program.DuplicateTrainingTemplate(original.Id);
        // Assert
        Assert.Equal(1, original.Order);
        Assert.Same(session, Assert.Single(original.TemplateSessions));
        Assert.Same(set, Assert.Single(session.TemplateExercises));
    }

    [Fact]
    public void DuplicateTrainingTemplate_PlacesTheCopyAtTheEndOfTheProgram()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate first = TrainingTemplate.Create(VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7, 1);
        TrainingTemplate second = TrainingTemplate.Create(VolumeLandmarks.MAV, TrainingDistribution.FullBody, 7, 2);
        program.AddTrainingTemplate(first);
        program.AddTrainingTemplate(second);
        // Act
        TrainingTemplate copy = program.DuplicateTrainingTemplate(first.Id);
        // Assert
        Assert.Equal(3, copy.Order);
        Assert.Equal(3, program.trainingTemplates.Count);
    }

    [Fact]
    public void DuplicateTrainingTemplate_DoesNotCopySoftDeletedSessionsAndSets()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate original = TrainingTemplate.Create(VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7, 1);
        TemplateSession activeSession = TemplateSession.Create("Active");
        TemplateSession deletedSession = TemplateSession.Create("Deleted");
        TemplateSet activeSet = CreateTemplateSet();
        TemplateSet deletedSet = CreateTemplateSet();
        activeSession.AddSet(activeSet);
        activeSession.AddSet(deletedSet);
        original.AddSessionTemplate(activeSession);
        original.AddSessionTemplate(deletedSession);
        program.AddTrainingTemplate(original);
        activeSession.DeleteTemplateSet(deletedSet.Id, Now);
        original.DeleteSessionTemplate(deletedSession.Id, Now);
        // Act
        TrainingTemplate copy = program.DuplicateTrainingTemplate(original.Id);
        // Assert
        TemplateSession copiedSession = Assert.Single(copy.TemplateSessions);
        Assert.Equal("Active", copiedSession.Name);
        TemplateSet copiedSet = Assert.Single(copiedSession.TemplateExercises);
        Assert.Equal(activeSet.ExerciseId, copiedSet.ExerciseId);
        Assert.False(copiedSet.IsDeleted);
    }

    [Fact]
    public void DuplicateTrainingTemplate_GivesNewSuperSetGroupIdsAndKeepsTheSetsGrouped()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate original = TrainingTemplate.Create(VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7, 1);
        TemplateSession session = TemplateSession.Create("Session");
        Guid superSetA = Guid.CreateVersion7();
        Guid superSetB = Guid.CreateVersion7();
        session.AddSet(CreateTemplateSet(superSetA));
        session.AddSet(CreateTemplateSet(superSetA));
        session.AddSet(CreateTemplateSet(superSetB));
        session.AddSet(CreateTemplateSet());
        original.AddSessionTemplate(session);
        program.AddTrainingTemplate(original);
        // Act
        TrainingTemplate copy = program.DuplicateTrainingTemplate(original.Id);
        // Assert
        List<TemplateSet> copiedSets = Assert.Single(copy.TemplateSessions).TemplateExercises.ToList();
        Guid? copiedA = copiedSets[0].SuperSetGroupId;
        Guid? copiedB = copiedSets[2].SuperSetGroupId;
        Assert.NotNull(copiedA);
        Assert.NotNull(copiedB);
        Assert.Equal(copiedA, copiedSets[1].SuperSetGroupId);
        Assert.NotEqual(copiedA, copiedB);
        Assert.NotEqual(superSetA, copiedA);
        Assert.NotEqual(superSetB, copiedB);
        Assert.Null(copiedSets[3].SuperSetGroupId);
    }

    [Fact]
    public void DuplicateTrainingTemplate_TheCopiedMuscleGroupsAreIndependentFromTheOriginal()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate original = TrainingTemplate.Create(VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7, 1);
        TemplateSession session = TemplateSession.Create("Session");
        TemplateSet set = CreateTemplateSet(muscleGroup: MuscleGroups.Back);
        session.AddSet(set);
        original.AddSessionTemplate(session);
        program.AddTrainingTemplate(original);
        // Act
        TrainingTemplate copy = program.DuplicateTrainingTemplate(original.Id);
        // Assert
        TemplateSet copiedSet = Assert.Single(Assert.Single(copy.TemplateSessions).TemplateExercises);
        Assert.NotSame(set.AimMuscleGroups, copiedSet.AimMuscleGroups);
    }

    [Fact]
    public void DuplicateTrainingTemplate_WithNonExistingTemplate_ThrowsDomainException()
    {
        // Arrange
        Program program = CreateProgram();
        // Act
        // Assert
        Assert.Throws<TrainingTemplateNotFoundDomainException>
        (
            () => program.DuplicateTrainingTemplate(Guid.CreateVersion7())
        );
    }

    [Fact]
    public void DuplicateTrainingTemplate_WithSoftDeletedTemplate_ThrowsDomainException()
    {
        // Arrange
        Program program = CreateProgram();
        TrainingTemplate template = TrainingTemplate.Create(VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7, 1);
        program.AddTrainingTemplate(template);
        program.DeleteTrainingTemplate(template.Id, Now);
        // Act
        // Assert
        Assert.Throws<TrainingTemplateNotFoundDomainException>
        (
            () => program.DuplicateTrainingTemplate(template.Id)
        );
    }
}
