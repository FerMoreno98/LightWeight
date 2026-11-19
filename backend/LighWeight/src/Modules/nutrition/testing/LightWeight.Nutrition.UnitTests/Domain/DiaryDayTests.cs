using LightWeight.Nutrition.Domain.Aggregates;
using LightWeight.Nutrition.Domain.Events;
using LightWeight.Nutrition.Domain.Exceptions;
using LightWeight.Nutrition.Domain.ValueObjects;

namespace LightWeight.Nutrition.UnitTests.Domain;

public class DiaryDayTests
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly DailyIntake Intake = DailyIntake.Create(2650m, 180m, 300m, 80m);

    [Fact]
    public void Create_WithValidDay_RecordsItAndRaisesTheEvent()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        DateOnly date = Today.AddDays(-1);
        // Act
        DiaryDay day = DiaryDay.Create(userId, date, Intake, Today);
        // Assert
        Assert.Equal(userId, day.UserId);
        Assert.Equal(date, day.Date);
        Assert.Equal(Intake, day.Intake);
        var @event = Assert.IsType<DiaryDayRecordedDomainEvent>(Assert.Single(day.DomainEvents));
        Assert.Equal(day.Id, @event.DiaryDayId);
        Assert.Equal(userId, @event.UserId);
        Assert.Equal(date, @event.Date);
        Assert.Equal(Intake, @event.Intake);
    }

    [Fact]
    public void Create_Today_IsAllowed()
    {
        DiaryDay day = DiaryDay.Create(Guid.CreateVersion7(), Today, Intake, Today);

        Assert.Equal(Today, day.Date);
    }

    [Fact]
    public void Create_InTheFuture_ThrowsDomainException()
    {
        Assert.Throws<DiaryDayInTheFutureDomainException>
        (
            () => DiaryDay.Create(Guid.CreateVersion7(), Today.AddDays(1), Intake, Today)
        );
    }

    [Fact]
    public void Create_WithoutUser_ThrowsDomainException()
    {
        Assert.Throws<UserIdEmptyDomainException>(() => DiaryDay.Create(Guid.Empty, Today, Intake, Today));
    }

    [Fact]
    public void Update_WithOtherTotals_ReplacesThemAndRaisesTheEvent()
    {
        // Arrange
        DiaryDay day = DiaryDay.Create(Guid.CreateVersion7(), Today, Intake, Today);
        day.ClearDomainEvents();
        DailyIntake newIntake = DailyIntake.Create(2100m, 150m, 220m, 70m);
        // Act
        bool changed = day.Update(newIntake);
        // Assert
        Assert.True(changed);
        Assert.Equal(newIntake, day.Intake);
        var @event = Assert.IsType<DiaryDayUpdatedDomainEvent>(Assert.Single(day.DomainEvents));
        Assert.Equal(day.Id, @event.DiaryDayId);
        Assert.Equal(day.Date, @event.Date);
        Assert.Equal(newIntake, @event.Intake);
    }

    [Fact]
    public void Update_WithTheSameTotals_DoesNotRaiseAnEvent()
    {
        // Arrange
        DiaryDay day = DiaryDay.Create(Guid.CreateVersion7(), Today, Intake, Today);
        day.ClearDomainEvents();
        // Act
        bool changed = day.Update(DailyIntake.Create(2650m, 180m, 300m, 80m));
        // Assert
        Assert.False(changed);
        Assert.Empty(day.DomainEvents);
    }

    [Fact]
    public void Delete_RaisesTheEvent()
    {
        // Arrange
        DiaryDay day = DiaryDay.Create(Guid.CreateVersion7(), Today, Intake, Today);
        day.ClearDomainEvents();
        // Act
        day.Delete();
        // Assert
        var @event = Assert.IsType<DiaryDayDeletedDomainEvent>(Assert.Single(day.DomainEvents));
        Assert.Equal(day.Id, @event.DiaryDayId);
        Assert.Equal(day.UserId, @event.UserId);
        Assert.Equal(day.Date, @event.Date);
    }
}
