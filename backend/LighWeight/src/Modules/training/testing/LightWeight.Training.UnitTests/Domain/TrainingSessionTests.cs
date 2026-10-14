using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.UnitTests.Domain;

public class TrainingSessionTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 18, 0, 0, DateTimeKind.Utc);

    private static TrainingSession StartSession()
    {
        return TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Push A", Now, "Con ganas", 8, 7, 3, Guid.CreateVersion7());
    }

    private static Set RegisterPlannedSet(TrainingSession session, Guid exerciseId, Guid? templateSetId)
    {
        return session.RegisterSet(exerciseId, templateSetId, AdvanceTrainingTechniques.None, null, 8, 80m, 8m, false);
    }

    // ---------------------------------------------------------------- Create

    [Fact]
    public void Create_WithValidData_StartsAnEmptySessionInProgress()
    {
        // Arrange
        var microcycleId = Guid.CreateVersion7();
        var templateSessionId = Guid.CreateVersion7();
        // Act
        TrainingSession session = TrainingSession.Create(microcycleId, Guid.CreateVersion7(), "Push A", Now, null, 8, 7, 3, templateSessionId);
        // Assert
        Assert.Equal(microcycleId, session.MicrocycleId);
        Assert.Equal(templateSessionId, session.TemplateSessionId);
        Assert.Equal(Now, session.StartAt);
        Assert.Null(session.FinishedAt);
        Assert.False(session.IsFinished);
        Assert.Equal(TimeSpan.Zero, session.Duration);
        Assert.Empty(session.Sets);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<UserIdEmptyDomainException>
        (
            () => TrainingSession.Create(Guid.CreateVersion7(), Guid.Empty, "Push A", Now, null, 8, 7, 3)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsDomainException(string name)
    {
        Assert.Throws<NameEmptyDomainException>
        (
            () => TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), name, Now, null, 8, 7, 3)
        );
    }

    // ---------------------------------------------------------------- RegisterSet

    [Fact]
    public void RegisterSet_PlannedSet_IsAddedWithItsValues()
    {
        // Arrange
        TrainingSession session = StartSession();
        var exerciseId = Guid.CreateVersion7();
        var templateSetId = Guid.CreateVersion7();
        var superSetGroupId = Guid.CreateVersion7();
        AdvanceTrainingTechniques dropSet = AdvanceTrainingTechniques.Create(true, false, false);
        // Act
        Set set = session.RegisterSet(exerciseId, templateSetId, dropSet, superSetGroupId, 7, 82.5m, 8.5m, false);
        // Assert
        Assert.Same(set, Assert.Single(session.Sets));
        Assert.Equal(exerciseId, set.ExerciseId);
        Assert.Equal(templateSetId, set.TemplateSetId);
        Assert.Equal(superSetGroupId, set.SuperSetGroupId);
        Assert.True(set.AdvanceTrainingTechniques.IsDropSet);
        Assert.Equal(7, set.Repetitions);
        Assert.Equal(82.5m, set.Weight);
        Assert.Equal(8.5m, set.RPE);
    }

    [Fact]
    public void RegisterSet_ReplacingTheExercise_KeepsTheLinkToThePlannedSet()
    {
        // Arrange
        TrainingSession session = StartSession();
        var substituteId = Guid.CreateVersion7();
        var templateSetId = Guid.CreateVersion7();
        // Act
        Set set = RegisterPlannedSet(session, substituteId, templateSetId);
        // Assert
        Assert.Equal(substituteId, set.ExerciseId);
        Assert.Equal(templateSetId, set.TemplateSetId);
    }

    [Fact]
    public void RegisterSet_ExtraSet_HasNoPlannedSet()
    {
        // Arrange
        TrainingSession session = StartSession();
        // Act
        Set first = RegisterPlannedSet(session, Guid.CreateVersion7(), null);
        Set second = RegisterPlannedSet(session, Guid.CreateVersion7(), null);
        // Assert
        Assert.Null(first.TemplateSetId);
        Assert.Null(second.TemplateSetId);
        Assert.Equal(2, session.Sets.Count);
    }

    [Fact]
    public void RegisterSet_PlannedSetAlreadyRegistered_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        var templateSetId = Guid.CreateVersion7();
        RegisterPlannedSet(session, Guid.CreateVersion7(), templateSetId);
        // Act
        // Assert
        Assert.Throws<TemplateSetAlreadyRegisteredDomainException>
        (
            () => RegisterPlannedSet(session, Guid.CreateVersion7(), templateSetId)
        );
        Assert.Single(session.Sets);
    }

    [Fact]
    public void RegisterSet_OnAFinishedSession_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        session.Finish(Now.AddHours(1));
        // Act
        // Assert
        Assert.Throws<TrainingSessionFinishedDomainException>
        (
            () => RegisterPlannedSet(session, Guid.CreateVersion7(), null)
        );
    }

    [Theory]
    [InlineData(-1, 80, 8)]
    [InlineData(8, -0.5, 8)]
    [InlineData(8, 80, 0.5)]
    [InlineData(8, 80, 10.5)]
    public void RegisterSet_WithInvalidValues_ThrowsDomainException(int repetitions, double weight, double rpe)
    {
        // Arrange
        TrainingSession session = StartSession();
        // Act
        // Assert
        Assert.Throws<InvalidSetValuesDomainException>
        (
            () => session.RegisterSet(Guid.CreateVersion7(), null, AdvanceTrainingTechniques.None, null, repetitions, (decimal)weight, (decimal)rpe, false)
        );
    }

    [Fact]
    public void RegisterSet_BodyWeightWithoutLoad_IsValid()
    {
        // Arrange
        TrainingSession session = StartSession();
        // Act
        Set set = session.RegisterSet(Guid.CreateVersion7(), null, AdvanceTrainingTechniques.None, null, 12, 0m, 9m, true);
        // Assert
        Assert.True(set.IsBodyWeight);
        Assert.Equal(0m, set.Weight);
    }

    // ---------------------------------------------------------------- UpdateSet / RemoveSet

    [Fact]
    public void UpdateSet_CorrectsTheValuesAndTheExercise()
    {
        // Arrange
        TrainingSession session = StartSession();
        var templateSetId = Guid.CreateVersion7();
        Set set = RegisterPlannedSet(session, Guid.CreateVersion7(), templateSetId);
        var substituteId = Guid.CreateVersion7();
        // Act
        session.UpdateSet(set.Id, substituteId, 6, 85m, 9m, false);
        // Assert
        Assert.Equal(substituteId, set.ExerciseId);
        Assert.Equal(6, set.Repetitions);
        Assert.Equal(85m, set.Weight);
        Assert.Equal(9m, set.RPE);
        Assert.Equal(templateSetId, set.TemplateSetId);
    }

    [Fact]
    public void UpdateSet_NotInTheSession_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        // Act
        // Assert
        Assert.Throws<PerformedSetNotFoundDomainException>
        (
            () => session.UpdateSet(Guid.CreateVersion7(), Guid.CreateVersion7(), 6, 85m, 9m, false)
        );
    }

    [Fact]
    public void UpdateSet_OnAFinishedSession_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        Set set = RegisterPlannedSet(session, Guid.CreateVersion7(), null);
        session.Finish(Now.AddHours(1));
        // Act
        // Assert
        Assert.Throws<TrainingSessionFinishedDomainException>
        (
            () => session.UpdateSet(set.Id, set.ExerciseId, 6, 85m, 9m, false)
        );
    }

    [Fact]
    public void RemoveSet_RemovesItAndFreesThePlannedSet()
    {
        // Arrange
        TrainingSession session = StartSession();
        var templateSetId = Guid.CreateVersion7();
        Set set = RegisterPlannedSet(session, Guid.CreateVersion7(), templateSetId);
        // Act
        session.RemoveSet(set.Id);
        // Assert
        Assert.Empty(session.Sets);
        RegisterPlannedSet(session, Guid.CreateVersion7(), templateSetId);
        Assert.Single(session.Sets);
    }

    [Fact]
    public void RemoveSet_NotInTheSession_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        // Act
        // Assert
        Assert.Throws<PerformedSetNotFoundDomainException>(() => session.RemoveSet(Guid.CreateVersion7()));
    }

    [Fact]
    public void RemoveSet_OnAFinishedSession_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        Set set = RegisterPlannedSet(session, Guid.CreateVersion7(), null);
        session.Finish(Now.AddHours(1));
        // Act
        // Assert
        Assert.Throws<TrainingSessionFinishedDomainException>(() => session.RemoveSet(set.Id));
    }

    // ---------------------------------------------------------------- Finish

    [Fact]
    public void Finish_StoresTheEndAndTheDuration()
    {
        // Arrange
        TrainingSession session = StartSession();
        DateTime finishedAt = Now.AddMinutes(75);
        // Act
        session.Finish(finishedAt);
        // Assert
        Assert.True(session.IsFinished);
        Assert.Equal(finishedAt, session.FinishedAt);
        Assert.Equal(TimeSpan.FromMinutes(75), session.Duration);
    }

    [Fact]
    public void Finish_AfterMidnight_CalculatesTheRealDuration()
    {
        // Arrange
        var lateStart = new DateTime(2026, 10, 14, 23, 30, 0, DateTimeKind.Utc);
        TrainingSession session = TrainingSession.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Noche", lateStart, null, 8, 7, 3);
        // Act
        session.Finish(lateStart.AddMinutes(90));
        // Assert
        Assert.Equal(TimeSpan.FromMinutes(90), session.Duration);
    }

    [Fact]
    public void Finish_Twice_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        session.Finish(Now.AddHours(1));
        // Act
        // Assert
        Assert.Throws<TrainingSessionFinishedDomainException>(() => session.Finish(Now.AddHours(2)));
    }

    // ---------------------------------------------------------------- EnsureCanBeDiscarded

    [Fact]
    public void EnsureCanBeDiscarded_InProgress_DoesNotThrow()
    {
        TrainingSession session = StartSession();
        session.EnsureCanBeDiscarded();
    }

    [Fact]
    public void EnsureCanBeDiscarded_Finished_ThrowsDomainException()
    {
        // Arrange
        TrainingSession session = StartSession();
        session.Finish(Now.AddHours(1));
        // Act
        // Assert
        Assert.Throws<TrainingSessionFinishedDomainException>(() => session.EnsureCanBeDiscarded());
    }
}
