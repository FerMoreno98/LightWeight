using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class SuperSetTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 18, 0, 0, DateTimeKind.Utc);

    private static List<TemplateSet> AddSets(TemplateSession session, Guid exerciseId, int count)
    {
        var sets = new List<TemplateSet>();
        for (var i = 0; i < count; i++)
        {
            TemplateSet set = TemplateSet.Create(exerciseId, RepetitionRange.Create(10, 8), 8, new List<MuscleGroups> { MuscleGroups.Chest });
            session.AddSet(set);
            sets.Add(set);
        }
        return sets;
    }

    private static IReadOnlyCollection<Guid> Ids(params IEnumerable<TemplateSet>[] groups) =>
        groups.SelectMany(g => g).Select(s => s.Id).ToList();

    // ---------------------------------------------------------------- TemplateSession.CreateSuperSet

    [Fact]
    public void CreateSuperSet_WithTwoExercises_GroupsAllTheirSets()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 3);
        List<TemplateSet> dips = AddSets(session, Guid.CreateVersion7(), 3);
        List<TemplateSet> bench = AddSets(session, Guid.CreateVersion7(), 2);
        // Act
        Guid superSetGroupId = session.CreateSuperSet(Ids(flyes, dips));
        // Assert
        Assert.All(flyes.Concat(dips), s => Assert.Equal(superSetGroupId, s.SuperSetGroupId));
        Assert.All(bench, s => Assert.Null(s.SuperSetGroupId));
    }

    [Fact]
    public void CreateSuperSet_WithASingleExercise_ThrowsDomainException()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 3);
        // Act
        // Assert
        Assert.Throws<SuperSetNeedsTwoExercisesDomainException>(() => session.CreateSuperSet(Ids(flyes)));
        Assert.All(flyes, s => Assert.Null(s.SuperSetGroupId));
    }

    [Fact]
    public void CreateSuperSet_WithASetOfAnotherSession_ThrowsDomainException()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        TemplateSession other = TemplateSession.Create("Pull");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 1);
        List<TemplateSet> rows = AddSets(other, Guid.CreateVersion7(), 1);
        // Act
        // Assert
        Assert.Throws<SetNotFoundDomainException>(() => session.CreateSuperSet(Ids(flyes, rows)));
    }

    [Fact]
    public void CreateSuperSet_WithADeletedSet_ThrowsDomainException()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 1);
        List<TemplateSet> dips = AddSets(session, Guid.CreateVersion7(), 1);
        session.DeleteTemplateSet(dips[0].Id, Now);
        // Act
        // Assert
        Assert.Throws<SetNotFoundDomainException>(() => session.CreateSuperSet(Ids(flyes, dips)));
    }

    [Fact]
    public void CreateSuperSet_TakingAnExerciseFromAnotherSuperSet_DissolvesTheOneLeftWithASingleExercise()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 2);
        List<TemplateSet> dips = AddSets(session, Guid.CreateVersion7(), 2);
        List<TemplateSet> raises = AddSets(session, Guid.CreateVersion7(), 2);
        session.CreateSuperSet(Ids(flyes, dips));
        // Act
        Guid newGroup = session.CreateSuperSet(Ids(dips, raises));
        // Assert
        Assert.All(dips.Concat(raises), s => Assert.Equal(newGroup, s.SuperSetGroupId));
        Assert.All(flyes, s => Assert.Null(s.SuperSetGroupId));
    }

    // ---------------------------------------------------------------- RemoveSuperSet / deleting sets

    [Fact]
    public void RemoveSuperSet_SeparatesAllItsSets()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 2);
        List<TemplateSet> dips = AddSets(session, Guid.CreateVersion7(), 2);
        Guid superSetGroupId = session.CreateSuperSet(Ids(flyes, dips));
        // Act
        session.RemoveSuperSet(superSetGroupId);
        // Assert
        Assert.All(flyes.Concat(dips), s => Assert.Null(s.SuperSetGroupId));
    }

    [Fact]
    public void RemoveSuperSet_NotInTheSession_ThrowsDomainException()
    {
        TemplateSession session = TemplateSession.Create("Push");
        Assert.Throws<SuperSetNotFoundDomainException>(() => session.RemoveSuperSet(Guid.CreateVersion7()));
    }

    [Fact]
    public void DeleteTemplateSet_LeavingASingleExercise_DissolvesTheSuperSet()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 2);
        List<TemplateSet> dips = AddSets(session, Guid.CreateVersion7(), 1);
        session.CreateSuperSet(Ids(flyes, dips));
        // Act
        session.DeleteTemplateSet(dips[0].Id, Now);
        // Assert
        Assert.All(flyes, s => Assert.Null(s.SuperSetGroupId));
    }

    [Fact]
    public void DeleteTemplateSet_KeepingTwoExercises_KeepsTheSuperSet()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 2);
        List<TemplateSet> dips = AddSets(session, Guid.CreateVersion7(), 2);
        Guid superSetGroupId = session.CreateSuperSet(Ids(flyes, dips));
        // Act
        session.DeleteTemplateSet(dips[0].Id, Now);
        // Assert
        Assert.All(flyes.Append(dips[1]), s => Assert.Equal(superSetGroupId, s.SuperSetGroupId));
    }

    [Fact]
    public void UpdateSet_KeepsTheSuperSet()
    {
        // Arrange
        TemplateSession session = TemplateSession.Create("Push");
        List<TemplateSet> flyes = AddSets(session, Guid.CreateVersion7(), 1);
        List<TemplateSet> dips = AddSets(session, Guid.CreateVersion7(), 1);
        Guid superSetGroupId = session.CreateSuperSet(Ids(flyes, dips));
        // Act
        flyes[0].UpdateSet(RepetitionRange.Create(15, 12), 9, new List<MuscleGroups> { MuscleGroups.Chest });
        // Assert
        Assert.Equal(superSetGroupId, flyes[0].SuperSetGroupId);
    }

    // ---------------------------------------------------------------- Real sets

    [Fact]
    public void RegisterSet_StoresTheSuperSetItWasPerformedIn()
    {
        // Arrange
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Push", Now, null, 8, 8, 2);
        var superSetGroupId = Guid.CreateVersion7();
        // Act
        Set set = session.RegisterSet(Guid.CreateVersion7(), null, AdvanceTrainingTechniques.None, superSetGroupId, 10, 20m, 8m, false);
        // Assert
        Assert.Equal(superSetGroupId, set.SuperSetGroupId);
    }

    [Fact]
    public void AssignSuperSet_JoinsAndSeparatesRegisteredSets()
    {
        // Arrange
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Push", Now, null, 8, 8, 2);
        Set first = session.RegisterSet(Guid.CreateVersion7(), null, AdvanceTrainingTechniques.None, null, 10, 20m, 8m, false);
        Set second = session.RegisterSet(Guid.CreateVersion7(), null, AdvanceTrainingTechniques.None, null, 10, 20m, 8m, false);
        var superSetGroupId = Guid.CreateVersion7();
        // Act
        session.AssignSuperSet(new[] { first.Id, second.Id }, superSetGroupId);
        // Assert
        Assert.Equal(superSetGroupId, first.SuperSetGroupId);
        Assert.Equal(superSetGroupId, second.SuperSetGroupId);
        // Act
        session.AssignSuperSet(new[] { first.Id, second.Id }, null);
        // Assert
        Assert.Null(first.SuperSetGroupId);
        Assert.Null(second.SuperSetGroupId);
    }

    [Fact]
    public void AssignSuperSet_OnAFinishedSession_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Push", Now, null, 8, 8, 2);
        Set set = session.RegisterSet(Guid.CreateVersion7(), null, AdvanceTrainingTechniques.None, null, 10, 20m, 8m, false);
        session.Finish(Now.AddHours(1));
        // Act
        // Assert
        Assert.Throws<TrainingSessionFinishedDomainException>(() => session.AssignSuperSet(new[] { set.Id }, Guid.CreateVersion7()));
    }

    [Fact]
    public void AssignSuperSet_WithASetOfAnotherSession_ThrowsDomainException()
    {
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Push", Now, null, 8, 8, 2);
        Assert.Throws<PerformedSetNotFoundDomainException>(() => session.AssignSuperSet(new[] { Guid.CreateVersion7() }, Guid.CreateVersion7()));
    }
}
