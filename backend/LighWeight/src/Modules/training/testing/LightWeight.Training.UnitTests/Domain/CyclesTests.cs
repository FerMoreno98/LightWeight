using LightWeight.Training.Domain.Aggregates;
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
        return macrocycle.PlanMesocycle(new List<Mesocycle>(), Guid.CreateVersion7(), 8, null, null, Now);
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
        previous.Finish(Now, new List<Mesocycle>());
        // Act
        Macrocycle macrocycle = Macrocycle.Create(previous.UserId, TrainingStage.Maintenance, null, Now.AddDays(1), previous);
        // Assert
        Assert.False(macrocycle.IsFinished);
    }

    [Fact]
    public void CreateMacrocycle_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<UserIdEmptyDomainException>
        (
            () => Macrocycle.Create(Guid.Empty, TrainingStage.Bulk, null, Now, null)
        );
    }

    // ---------------------------------------------------------------- PlanMesocycle

    [Fact]
    public void PlanMesocycle_FirstMesocycle_StartsItNowLinkedToTheMacrocycleAndProgram()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        var programId = Guid.CreateVersion7();
        // Act
        Mesocycle mesocycle = macrocycle.PlanMesocycle(new List<Mesocycle>(), programId, 9, "Hombro", "Notas", Now);
        // Assert
        Assert.Equal(macrocycle.Id, mesocycle.MacrocycleId);
        Assert.Equal(macrocycle.UserId, mesocycle.UserId);
        Assert.Equal(programId, mesocycle.ProgramId);
        Assert.Equal(9, mesocycle.MotivationLevel);
        Assert.Equal("Hombro", mesocycle.Injuries);
        Assert.Equal(Now, mesocycle.StartedAt);
        Assert.False(mesocycle.IsFinished);
    }

    [Fact]
    public void PlanMesocycle_WithAnActiveMesocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle active = PlanFirstMesocycle(macrocycle);
        // Act
        // Assert
        Assert.Throws<ActiveMesocycleAlreadyExistsDomainException>
        (
            () => macrocycle.PlanMesocycle(new List<Mesocycle> { active }, Guid.CreateVersion7(), 8, null, null, Now)
        );
    }

    [Fact]
    public void PlanMesocycle_AfterFinishingThePreviousOne_StartsTheNextOne()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle previous = PlanFirstMesocycle(macrocycle);
        previous.Finish(Now.AddDays(28));
        // Act
        Mesocycle next = macrocycle.PlanMesocycle(new List<Mesocycle> { previous }, Guid.CreateVersion7(), 8, null, null, Now.AddDays(29));
        // Assert
        Assert.False(next.IsFinished);
        Assert.NotEqual(previous.Id, next.Id);
    }

    [Fact]
    public void PlanMesocycle_InAFinishedMacrocycle_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        macrocycle.Finish(Now, new List<Mesocycle>());
        // Act
        // Assert
        Assert.Throws<MacrocycleFinishedDomainException>
        (
            () => macrocycle.PlanMesocycle(new List<Mesocycle>(), Guid.CreateVersion7(), 8, null, null, Now)
        );
    }

    // ---------------------------------------------------------------- Finish

    [Fact]
    public void FinishMacrocycle_FinishesItsActiveMesocycleWithTheSameDate()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        Mesocycle finished = PlanFirstMesocycle(macrocycle);
        finished.Finish(Now.AddDays(28));
        Mesocycle active = macrocycle.PlanMesocycle(new List<Mesocycle> { finished }, Guid.CreateVersion7(), 8, null, null, Now.AddDays(29));
        DateTime end = Now.AddDays(60);
        // Act
        macrocycle.Finish(end, new List<Mesocycle> { finished, active });
        // Assert
        Assert.Equal(end, macrocycle.FinishedAt);
        Assert.Equal(end, active.FinishedAt);
        Assert.Equal(Now.AddDays(28), finished.FinishedAt);
    }

    [Fact]
    public void FinishMacrocycle_Twice_ThrowsDomainException()
    {
        // Arrange
        Macrocycle macrocycle = CreateActiveMacrocycle();
        macrocycle.Finish(Now, new List<Mesocycle>());
        // Act
        // Assert
        Assert.Throws<MacrocycleFinishedDomainException>(() => macrocycle.Finish(Now.AddDays(1), new List<Mesocycle>()));
        Assert.Equal(Now, macrocycle.FinishedAt);
    }

    [Fact]
    public void FinishMesocycle_Twice_ThrowsDomainException()
    {
        // Arrange
        Mesocycle mesocycle = PlanFirstMesocycle(CreateActiveMacrocycle());
        mesocycle.Finish(Now);
        // Act
        // Assert
        Assert.Throws<MesocycleFinishedDomainException>(() => mesocycle.Finish(Now.AddDays(1)));
        Assert.Equal(Now, mesocycle.FinishedAt);
    }

    // ---------------------------------------------------------------- PlanMicrocycle

    [Fact]
    public void PlanMicrocycle_FirstWeek_IsWeekOne()
    {
        // Arrange
        Mesocycle mesocycle = PlanFirstMesocycle(CreateActiveMacrocycle());
        var templateId = Guid.CreateVersion7();
        // Act
        Microcycle microcycle = mesocycle.PlanMicrocycle(new List<Microcycle>(), templateId);
        // Assert
        Assert.Equal(1, microcycle.WeekNumber);
        Assert.Equal(mesocycle.Id, microcycle.MesocycleId);
        Assert.Equal(mesocycle.UserId, microcycle.UserId);
        Assert.Equal(templateId, microcycle.TrainingTemplateId);
    }

    [Fact]
    public void PlanMicrocycle_WithPreviousWeeks_IsTheWeekAfterTheLastOne()
    {
        // Arrange
        Mesocycle mesocycle = PlanFirstMesocycle(CreateActiveMacrocycle());
        Microcycle week1 = mesocycle.PlanMicrocycle(new List<Microcycle>(), Guid.CreateVersion7());
        Microcycle week2 = mesocycle.PlanMicrocycle(new List<Microcycle> { week1 }, Guid.CreateVersion7());
        // Act
        Microcycle week3 = mesocycle.PlanMicrocycle(new List<Microcycle> { week2, week1 }, Guid.CreateVersion7());
        // Assert
        Assert.Equal(2, week2.WeekNumber);
        Assert.Equal(3, week3.WeekNumber);
    }

    [Fact]
    public void PlanMicrocycle_InAFinishedMesocycle_ThrowsDomainException()
    {
        // Arrange
        Mesocycle mesocycle = PlanFirstMesocycle(CreateActiveMacrocycle());
        mesocycle.Finish(Now);
        // Act
        // Assert
        Assert.Throws<MesocycleFinishedDomainException>
        (
            () => mesocycle.PlanMicrocycle(new List<Microcycle>(), Guid.CreateVersion7())
        );
    }
}
