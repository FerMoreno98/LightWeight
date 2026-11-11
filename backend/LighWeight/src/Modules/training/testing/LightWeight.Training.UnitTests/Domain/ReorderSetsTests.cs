using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class ReorderSetsTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    private static TemplateSet CreateSet(Guid exerciseId) =>
        TemplateSet.Create(exerciseId, RepetitionRange.Create(8, 12), 8, new List<MuscleGroups> { MuscleGroups.Chest });

    /// <summary>Press x2, curl x1, pull-ups x2, created in this order</summary>
    private sealed class Scenario
    {
        public Guid Press { get; } = Guid.NewGuid();
        public Guid Curl { get; } = Guid.NewGuid();
        public Guid PullUp { get; } = Guid.NewGuid();
        public TemplateSession Session { get; } = TemplateSession.Create("Torso A");
        public List<TemplateSet> Sets { get; } = new();

        public Scenario()
        {
            foreach (Guid exercise in new[] { Press, Press, Curl, PullUp, PullUp })
            {
                TemplateSet set = CreateSet(exercise);
                Session.AddSet(set);
                Sets.Add(set);
            }
        }

        public List<Guid> Ids(params int[] positions) => positions.Select(p => Sets[p].Id).ToList();
    }

    [Fact]
    public void ReorderSets_SetsTheNewOrder()
    {
        // Arrange
        var s = new Scenario();
        List<Guid> newOrder = s.Ids(2, 3, 4, 0, 1);
        // Act
        s.Session.ReorderSets(newOrder);
        // Assert
        Assert.Equal(newOrder, s.Session.TemplateExercises.Select(x => x.Id));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, s.Session.TemplateExercises.Select(x => x.Order));
        Assert.Equal(new[] { s.Curl, s.PullUp, s.PullUp, s.Press, s.Press }, s.Session.TemplateExercises.Select(x => x.ExerciseId));
    }

    [Fact]
    public void ReorderSets_KeepsTheSuperSets()
    {
        // Arrange
        var s = new Scenario();
        Guid superSet = s.Session.CreateSuperSet(s.Ids(0, 1, 3, 4));
        // Act: the superset (press + pull-ups) after the curl
        s.Session.ReorderSets(s.Ids(2, 0, 1, 3, 4));
        // Assert
        List<TemplateSet> ordered = s.Session.TemplateExercises.ToList();
        Assert.Null(ordered[0].SuperSetGroupId);
        Assert.All(ordered.Skip(1), x => Assert.Equal(superSet, x.SuperSetGroupId));
    }

    [Fact]
    public void ReorderSets_WithAForeignSet_Throws()
    {
        // Arrange
        var s = new Scenario();
        List<Guid> newOrder = s.Ids(0, 1, 2, 3);
        newOrder.Add(Guid.NewGuid());
        // Act
        // Assert
        Assert.Throws<InvalidSetOrderDomainException>(() => s.Session.ReorderSets(newOrder));
    }

    [Fact]
    public void ReorderSets_WithADuplicatedSet_Throws()
    {
        // Arrange
        var s = new Scenario();
        // Act
        // Assert
        Assert.Throws<InvalidSetOrderDomainException>(() => s.Session.ReorderSets(s.Ids(0, 0, 1, 2, 3)));
    }

    [Fact]
    public void ReorderSets_WithAMissingSet_Throws()
    {
        // Arrange
        var s = new Scenario();
        // Act
        // Assert
        Assert.Throws<InvalidSetOrderDomainException>(() => s.Session.ReorderSets(s.Ids(0, 1, 2, 3)));
    }

    [Fact]
    public void ReorderSets_WithADeletedSet_Throws()
    {
        // Arrange
        var s = new Scenario();
        s.Session.DeleteTemplateSet(s.Sets[4].Id, Now);
        // Act
        // Assert
        Assert.Throws<InvalidSetOrderDomainException>(() => s.Session.ReorderSets(s.Ids(4, 3, 2, 1, 0)));
    }

    [Fact]
    public void ReorderSets_WhenItFails_LeavesTheOrderUnchanged()
    {
        // Arrange
        var s = new Scenario();
        List<Guid> before = s.Session.TemplateExercises.Select(x => x.Id).ToList();
        // Act
        Assert.Throws<InvalidSetOrderDomainException>(() => s.Session.ReorderSets(s.Ids(4, 3, 2, 1)));
        // Assert
        Assert.Equal(before, s.Session.TemplateExercises.Select(x => x.Id));
    }

    [Fact]
    public void ReorderSets_IgnoresDeletedSetsAndANewSetGoesAtTheEnd()
    {
        // Arrange
        var s = new Scenario();
        s.Session.DeleteTemplateSet(s.Sets[0].Id, Now);
        s.Session.ReorderSets(s.Ids(4, 3, 2, 1));
        // Act
        TemplateSet added = CreateSet(Guid.NewGuid());
        s.Session.AddSet(added);
        // Assert
        Assert.Equal(added.Id, s.Session.TemplateExercises.Last().Id);
        Assert.Equal(s.Ids(4, 3, 2, 1).Append(added.Id), s.Session.TemplateExercises.Select(x => x.Id));
    }
}
