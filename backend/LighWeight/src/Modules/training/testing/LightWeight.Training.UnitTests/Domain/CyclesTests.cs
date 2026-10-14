using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.UnitTests.Domain;

public class CyclesTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 10, 0, 0, DateTimeKind.Utc);

    private static Macrocycle CreateActiveMacrocycle()
    {
        return Macrocycle.Create(Guid.CreateVersion7(), TrainingStage.Bulk, null, Now, null);
    }

    private static Mesocycle PlanFirstMesocycle(Macrocycle macrocycle)
    {
        return macrocycle.PlanMesocycle(Guid.CreateVersion7(), 8, null, null, Now);
    }

    // ---------------------------------------------------------------- Macrocycle.Create

    [Fact]
    public void CreateMacrocycle_WithoutActiveMacrocycle_StartsItNow()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        // Act
        Macrocycle macrocycle = Macrocycle.Create(userId, TrainingStage.Cut, "Definición", Now, null);
        // Assert
        Assert.Equal(userId, macrocycle.UserId);
        Assert.Equal(TrainingStage.Cut, macrocycle.Stage);
        Assert.Equal("Definición", macrocycle.Comments);
        Assert.Equal(Now, macrocycle.StartedAt);
        Assert.Null(macrocycle.FinishedAt);
        Assert.False(macrocycle.IsFinished);
        Assert.Empty(macrocycle.Mesocycles);
    }

    [Fact]
    public void CreateMacrocycle_WithAnActiveMacrocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle active = CreateActiveMacrocycle();
        // Act
        // Assert
        Assert.Throws<ActiveMacrocycleAlreadyExistsDomainException>
        (
            () => Macrocycle.Create(active.UserId, TrainingStage.Bulk, null, Now, active)
        );
    }

    [Fact]
    public void CreateMacrocycle_AfterFinishingThePreviousOne_StartsANewOne()
    {
        // Arrange
        Macrocycle previous = CreateActiveMacrocycle();
        previous.Finish(Now);
        // Act
        Macrocycle macrocycle = Macrocycle.Create(previous.UserId, TrainingStage.Maintenance, null, Now.AddDays(1), previous);
        // Assert
        Assert.False(macrocycle.IsFinished);
    }

    [Fact]
    public void CreateMacrocycle_WithEmptyUserId_ThrowsDomainException()
    {
        Assert.Throws<UserIdEmptyDomainException>
        (
            () => Macrocycle.Create(Guid.Empty, TrainingStage.Bulk, null, Now, null)
        );
    }

    // ---------------------------------------------------------------- PlanMesocycle

    [Fact]
    public void PlanMesocycle_FirstMesocycle_IsAddedToTheMacrocycle()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        var programId = Guid.CreateVersion7();
        // Act
        Mesocycle mesocycle = macrocycle.PlanMesocycle(programId, 7, "Hombro", "Notas", Now);
        // Assert
        Assert.Single(macrocycle.Mesocycles);
        Assert.Same(mesocycle, macrocycle.ActiveMesocycle);
        Assert.Equal(macrocycle.Id, mesocycle.MacrocycleId);
        Assert.Equal(macrocycle.UserId, mesocycle.UserId);
        Assert.Equal(programId, mesocycle.ProgramId);
        Assert.Equal(7, mesocycle.MotivationLevel);
        Assert.Equal("Hombro", mesocycle.Injuries);
        Assert.Equal(Now, mesocycle.StartedAt);
        Assert.False(mesocycle.IsFinished);
    }

    [Fact]
    public void PlanMesocycle_WithAnActiveMesocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        PlanFirstMesocycle(macrocycle);
        // Act
        // Assert
        Assert.Throws<ActiveMesocycleAlreadyExistsDomainException>(() => PlanFirstMesocycle(macrocycle));
    }

    [Fact]
    public void PlanMesocycle_AfterFinishingTheActiveOne_StartsANewOne()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle first = PlanFirstMesocycle(macrocycle);
        macrocycle.FinishMesocycle(first.Id, Now.AddDays(30));
        // Act
        Mesocycle second = PlanFirstMesocycle(macrocycle);
        // Assert
        Assert.Equal(2, macrocycle.Mesocycles.Count);
        Assert.Same(second, macrocycle.ActiveMesocycle);
    }

    [Fact]
    public void PlanMesocycle_OnAFinishedMacrocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        macrocycle.Finish(Now);
        // Act
        // Assert
        Assert.Throws<MacrocycleFinishedDomainException>(() => PlanFirstMesocycle(macrocycle));
    }

    // ---------------------------------------------------------------- Finish

    [Fact]
    public void FinishMacrocycle_FinishesItsActiveMesocycleAndWeekWithTheSameDate()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        DateTime finishedAt = Now.AddDays(90);
        // Act
        macrocycle.Finish(finishedAt);
        // Assert
        Assert.Equal(finishedAt, macrocycle.FinishedAt);
        Assert.Equal(finishedAt, mesocycle.FinishedAt);
        Assert.Equal(finishedAt, week.FinishedAt);
        Assert.Null(macrocycle.ActiveMesocycle);
        Assert.Null(macrocycle.ActiveMicrocycle);
    }

    [Fact]
    public void FinishMacrocycle_KeepsTheDateOfMesocyclesAlreadyFinished()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle first = PlanFirstMesocycle(macrocycle);
        DateTime firstFinishedAt = Now.AddDays(30);
        macrocycle.FinishMesocycle(first.Id, firstFinishedAt);
        // Act
        macrocycle.Finish(Now.AddDays(90));
        // Assert
        Assert.Equal(firstFinishedAt, first.FinishedAt);
    }

    [Fact]
    public void FinishMacrocycle_Twice_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        macrocycle.Finish(Now);
        // Act
        // Assert
        Assert.Throws<MacrocycleFinishedDomainException>(() => macrocycle.Finish(Now));
    }

    [Fact]
    public void FinishMesocycle_FinishesItsActiveWeekWithTheSameDate()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        DateTime finishedAt = Now.AddDays(30);
        // Act
        macrocycle.FinishMesocycle(mesocycle.Id, finishedAt);
        // Assert
        Assert.Equal(finishedAt, mesocycle.FinishedAt);
        Assert.Equal(finishedAt, week.FinishedAt);
        Assert.False(macrocycle.IsFinished);
    }

    [Fact]
    public void FinishMesocycle_Twice_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        macrocycle.FinishMesocycle(mesocycle.Id, Now);
        // Act
        // Assert
        Assert.Throws<MesocycleFinishedDomainException>(() => macrocycle.FinishMesocycle(mesocycle.Id, Now));
    }

    [Fact]
    public void FinishMesocycle_FromAnotherMacrocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        // Act
        // Assert
        Assert.Throws<MesocycleNotFoundDomainException>(() => macrocycle.FinishMesocycle(Guid.CreateVersion7(), Now));
    }

    // ---------------------------------------------------------------- PlanMicrocycle

    [Fact]
    public void PlanMicrocycle_FirstWeek_IsWeekOne()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        var templateId = Guid.CreateVersion7();
        // Act
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, templateId, Now);
        // Assert
        Assert.Equal(1, week.WeekNumber);
        Assert.Equal(templateId, week.TrainingTemplateId);
        Assert.Equal(mesocycle.Id, week.MesocycleId);
        Assert.Equal(macrocycle.UserId, week.UserId);
        Assert.Equal(Now, week.StartedAt);
        Assert.False(week.IsFinished);
        Assert.Same(week, macrocycle.ActiveMicrocycle);
    }

    [Fact]
    public void PlanMicrocycle_AfterFinishingTheActiveWeek_IsTheNextWeek()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle first = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        macrocycle.FinishMicrocycle(first.Id, Now.AddDays(7));
        // Act
        Microcycle second = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now.AddDays(7));
        // Assert
        Assert.Equal(2, second.WeekNumber);
        Assert.Equal(2, mesocycle.Microcycles.Count);
        Assert.Same(second, macrocycle.ActiveMicrocycle);
    }

    [Fact]
    public void PlanMicrocycle_WithAnActiveWeek_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        // Act
        // Assert
        Assert.Throws<ActiveMicrocycleAlreadyExistsDomainException>
        (
            () => macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now)
        );
    }

    [Fact]
    public void PlanMicrocycle_OnAFinishedMesocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        macrocycle.FinishMesocycle(mesocycle.Id, Now);
        // Act
        // Assert
        Assert.Throws<MesocycleFinishedDomainException>
        (
            () => macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now)
        );
    }

    [Fact]
    public void PlanMicrocycle_OnAMesocycleOfAnotherMacrocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        // Act
        // Assert
        Assert.Throws<MesocycleNotFoundDomainException>
        (
            () => macrocycle.PlanMicrocycle(Guid.CreateVersion7(), Guid.CreateVersion7(), Now)
        );
    }

    // ---------------------------------------------------------------- FinishMicrocycle

    [Fact]
    public void FinishMicrocycle_FinishesOnlyTheWeek()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        DateTime finishedAt = Now.AddDays(7);
        // Act
        macrocycle.FinishMicrocycle(week.Id, finishedAt);
        // Assert
        Assert.Equal(finishedAt, week.FinishedAt);
        Assert.Null(macrocycle.ActiveMicrocycle);
        Assert.False(mesocycle.IsFinished);
    }

    [Fact]
    public void FinishMicrocycle_Twice_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        macrocycle.FinishMicrocycle(week.Id, Now);
        // Act
        // Assert
        Assert.Throws<MicrocycleFinishedDomainException>(() => macrocycle.FinishMicrocycle(week.Id, Now));
    }

    [Fact]
    public void FinishMicrocycle_FromAnotherMacrocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        PlanFirstMesocycle(macrocycle);
        // Act
        // Assert
        Assert.Throws<MicrocycleNotFoundDomainException>(() => macrocycle.FinishMicrocycle(Guid.CreateVersion7(), Now));
    }

    // ---------------------------------------------------------------- FinishMicrocycleIfCompleted

    [Fact]
    public void FinishMicrocycleIfCompleted_AllPlannedSessionsFinished_FinishesTheWeek()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        var push = Guid.CreateVersion7();
        var pull = Guid.CreateVersion7();
        // Act
        bool finished = macrocycle.FinishMicrocycleIfCompleted(week.Id, new[] { push, pull }, new[] { pull, push }, Now.AddDays(3));
        // Assert
        Assert.True(finished);
        Assert.Equal(Now.AddDays(3), week.FinishedAt);
        Assert.False(mesocycle.IsFinished);
    }

    [Fact]
    public void FinishMicrocycleIfCompleted_SomeSessionPending_KeepsTheWeekInProgress()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        var push = Guid.CreateVersion7();
        // Act
        bool finished = macrocycle.FinishMicrocycleIfCompleted(week.Id, new[] { push, Guid.CreateVersion7() }, new[] { push }, Now);
        // Assert
        Assert.False(finished);
        Assert.False(week.IsFinished);
    }

    [Fact]
    public void FinishMicrocycleIfCompleted_TemplateWithoutSessions_NeverFinishesTheWeek()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        // Act
        bool finished = macrocycle.FinishMicrocycleIfCompleted(week.Id, Array.Empty<Guid>(), new[] { Guid.CreateVersion7() }, Now);
        // Assert
        Assert.False(finished);
        Assert.False(week.IsFinished);
    }

    [Fact]
    public void FinishMicrocycleIfCompleted_WeekAlreadyFinished_DoesNothing()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle mesocycle = PlanFirstMesocycle(macrocycle);
        Microcycle week = macrocycle.PlanMicrocycle(mesocycle.Id, Guid.CreateVersion7(), Now);
        macrocycle.FinishMicrocycle(week.Id, Now);
        var push = Guid.CreateVersion7();
        // Act
        bool finished = macrocycle.FinishMicrocycleIfCompleted(week.Id, new[] { push }, new[] { push }, Now.AddDays(1));
        // Assert
        Assert.False(finished);
        Assert.Equal(Now, week.FinishedAt);
    }
}
