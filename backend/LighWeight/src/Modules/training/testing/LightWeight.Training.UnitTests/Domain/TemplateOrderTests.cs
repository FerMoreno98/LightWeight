using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

/// <summary>
/// Sessions and sets keep an explicit Order: ids created in the same millisecond (duplicating, copying, several
/// series at once) do not sort in creation order, so the order can never depend on them
/// </summary>
public class TemplateOrderTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    private static TemplateSet CreateSet(Guid exerciseId) =>
        TemplateSet.Create(exerciseId, RepetitionRange.Create(8, 12), 8, new List<MuscleGroups> { MuscleGroups.Chest });

    private static TrainingTemplate CreateTemplate() =>
        TrainingTemplate.Create("Plantilla", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);

    /// <summary>A session with many sets of three exercises, created as fast as possible (same millisecond)</summary>
    private static (TemplateSession Session, List<Guid> Exercises) CreateSessionWithManySets()
    {
        TemplateSession session = TemplateSession.Create("Torso A");
        var exercises = new List<Guid>();
        foreach (Guid exercise in new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() })
            for (int i = 0; i < 10; i++)
            {
                session.AddSet(CreateSet(exercise));
                exercises.Add(exercise);
            }
        return (session, exercises);
    }

    [Fact]
    public void AddSet_AssignsConsecutiveOrders()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Torso A");
        // Act
        session.AddSet(CreateSet(Guid.NewGuid()));
        session.AddSet(CreateSet(Guid.NewGuid()));
        session.AddSet(CreateSet(Guid.NewGuid()));
        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, session.TemplateExercises.Select(s => s.Order));
    }

    [Fact]
    public void AddSet_AfterDeletingTheLast_DoesNotReuseItsOrder()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Torso A");
        session.AddSet(CreateSet(Guid.NewGuid()));
        TemplateSet last = CreateSet(Guid.NewGuid());
        session.AddSet(last);
        session.DeleteTemplateSet(last.Id, Now);
        // Act
        TemplateSet added = CreateSet(Guid.NewGuid());
        session.AddSet(added);
        // Assert
        Assert.Equal(3, added.Order);
    }

    [Fact]
    public void AddSessionTemplate_AssignsConsecutiveOrders()
    {
        // Arrange
        TrainingTemplate template = CreateTemplate();
        // Act
        template.AddSessionTemplate(TemplateSession.Create("Torso A"));
        template.AddSessionTemplate(TemplateSession.Create("Pierna A"));
        template.AddSessionTemplate(TemplateSession.Create("Torso B"));
        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, template.TemplateSessions.Select(s => s.Order));
        Assert.Equal(new[] { "Torso A", "Pierna A", "Torso B" }, template.TemplateSessions.Select(s => s.Name));
    }

    [Fact]
    public void DuplicateTrainingTemplate_KeepsTheOrderOfSessionsAndSets()
    {
        // Arrange
        Program program = Program.Create(Guid.CreateVersion7(), Periodization.Linear, "Programa", new List<MuscleGroups>());
        TrainingTemplate template = CreateTemplate();
        (TemplateSession session, List<Guid> exercises) = CreateSessionWithManySets();
        template.AddSessionTemplate(session);
        template.AddSessionTemplate(TemplateSession.Create("Pierna A"));
        program.AddTrainingTemplate(template);
        // Act
        TrainingTemplate copy = program.DuplicateTrainingTemplate(template.Id);
        // Assert
        Assert.Equal(new[] { "Torso A", "Pierna A" }, copy.TemplateSessions.Select(s => s.Name));
        Assert.Equal(new[] { 1, 2 }, copy.TemplateSessions.Select(s => s.Order));
        TemplateSession copiedSession = copy.TemplateSessions.First();
        Assert.Equal(exercises, copiedSession.TemplateExercises.Select(s => s.ExerciseId));
        Assert.Equal(Enumerable.Range(1, exercises.Count), copiedSession.TemplateExercises.Select(s => s.Order));
    }

    [Fact]
    public void CopyFor_KeepsTheOrderOfSessionsAndSets()
    {
        // Arrange
        Program program = Program.Create(Guid.CreateVersion7(), Periodization.Linear, "Programa", new List<MuscleGroups>());
        TrainingTemplate template = CreateTemplate();
        (TemplateSession session, List<Guid> exercises) = CreateSessionWithManySets();
        template.AddSessionTemplate(TemplateSession.Create("Calentamiento"));
        template.AddSessionTemplate(session);
        program.AddTrainingTemplate(template);
        // Act
        Program copy = program.CopyFor(Guid.CreateVersion7());
        // Assert
        TrainingTemplate copiedTemplate = Assert.Single(copy.trainingTemplates);
        Assert.Equal(new[] { "Calentamiento", "Torso A" }, copiedTemplate.TemplateSessions.Select(s => s.Name));
        TemplateSession copiedSession = copiedTemplate.TemplateSessions.Last();
        Assert.Equal(exercises, copiedSession.TemplateExercises.Select(s => s.ExerciseId));
        Assert.Equal(Enumerable.Range(1, exercises.Count), copiedSession.TemplateExercises.Select(s => s.Order));
    }
}
