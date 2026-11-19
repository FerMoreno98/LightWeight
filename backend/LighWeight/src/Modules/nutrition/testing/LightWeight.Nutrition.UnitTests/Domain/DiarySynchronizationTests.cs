using LightWeight.Nutrition.Domain.Aggregates;
using LightWeight.Nutrition.Domain.Events;
using LightWeight.Nutrition.Domain.Exceptions;
using LightWeight.Nutrition.Domain.Services;
using LightWeight.Nutrition.Domain.ValueObjects;

namespace LightWeight.Nutrition.UnitTests.Domain;

public class DiarySynchronizationTests
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly DiaryPeriod LastWeek = DiaryPeriod.Create(Today.AddDays(-6), Today);
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static DailyIntake Kcal(decimal calories) => DailyIntake.Create(calories, 150m, 250m, 70m);

    private static DiaryDay Recorded(DateOnly date, decimal calories)
    {
        DiaryDay day = DiaryDay.Create(UserId, date, Kcal(calories), Today);
        day.ClearDomainEvents();
        return day;
    }

    [Fact]
    public void Apply_WithNewDays_CreatesThemInOrder()
    {
        // Arrange
        SyncedDay[] synced = [new(Today, Kcal(2000m)), new(Today.AddDays(-2), Kcal(2500m))];
        // Act
        DiarySynchronizationResult result = DiarySynchronization.Apply(UserId, LastWeek, [], synced, Today);
        // Assert
        Assert.Empty(result.Deleted);
        Assert.Collection
        (
            result.Created,
            day => { Assert.Equal(Today.AddDays(-2), day.Date); Assert.Equal(2500m, day.Intake.Calories); },
            day => { Assert.Equal(Today, day.Date); Assert.Equal(2000m, day.Intake.Calories); }
        );
        Assert.All(result.Created, day => Assert.Equal(UserId, day.UserId));
        Assert.All(result.Created, day => Assert.IsType<DiaryDayRecordedDomainEvent>(Assert.Single(day.DomainEvents)));
    }

    [Fact]
    public void Apply_WithAChangedDay_UpdatesItWithoutListingIt()
    {
        // Arrange
        DiaryDay yesterday = Recorded(Today.AddDays(-1), 2000m);
        // Act
        DiarySynchronizationResult result = DiarySynchronization.Apply
        (
            UserId, LastWeek, [yesterday], [new SyncedDay(yesterday.Date, Kcal(2300m))], Today
        );
        // Assert
        Assert.Empty(result.Created);
        Assert.Empty(result.Deleted);
        Assert.Equal(2300m, yesterday.Intake.Calories);
        Assert.IsType<DiaryDayUpdatedDomainEvent>(Assert.Single(yesterday.DomainEvents));
    }

    [Fact]
    public void Apply_WithAnUnchangedDay_RaisesNoEvent()
    {
        // Arrange
        DiaryDay yesterday = Recorded(Today.AddDays(-1), 2000m);
        // Act
        DiarySynchronization.Apply(UserId, LastWeek, [yesterday], [new SyncedDay(yesterday.Date, Kcal(2000m))], Today);
        // Assert
        Assert.Empty(yesterday.DomainEvents);
    }

    [Fact]
    public void Apply_WithARecordedDayThatDidNotArrive_DeletesIt()
    {
        // Arrange
        DiaryDay emptied = Recorded(Today.AddDays(-3), 1800m);
        DiaryDay kept = Recorded(Today.AddDays(-1), 2000m);
        // Act
        DiarySynchronizationResult result = DiarySynchronization.Apply
        (
            UserId, LastWeek, [emptied, kept], [new SyncedDay(kept.Date, Kcal(2000m))], Today
        );
        // Assert
        Assert.Same(emptied, Assert.Single(result.Deleted));
        Assert.IsType<DiaryDayDeletedDomainEvent>(Assert.Single(emptied.DomainEvents));
        Assert.Empty(kept.DomainEvents);
        Assert.Empty(result.Created);
    }

    [Fact]
    public void Apply_WithNothingSynced_DeletesEveryRecordedDay()
    {
        DiaryDay[] recorded = [Recorded(Today.AddDays(-2), 2000m), Recorded(Today, 1500m)];

        DiarySynchronizationResult result = DiarySynchronization.Apply(UserId, LastWeek, recorded, [], Today);

        Assert.Equal(2, result.Deleted.Count);
    }

    [Fact]
    public void Apply_WithASyncedDayOutOfThePeriod_ThrowsDomainException()
    {
        Assert.Throws<DiaryDayOutOfPeriodDomainException>
        (
            () => DiarySynchronization.Apply(UserId, LastWeek, [], [new SyncedDay(Today.AddDays(-7), Kcal(2000m))], Today)
        );
    }

    [Fact]
    public void Apply_WithARecordedDayOutOfThePeriod_ThrowsDomainException()
    {
        Assert.Throws<DiaryDayOutOfPeriodDomainException>
        (
            () => DiarySynchronization.Apply(UserId, LastWeek, [Recorded(Today.AddDays(-10), 2000m)], [], Today)
        );
    }

    [Fact]
    public void Apply_WithARepeatedSyncedDay_ThrowsDomainException()
    {
        Assert.Throws<DuplicatedDiaryDayDomainException>
        (
            () => DiarySynchronization.Apply
            (
                UserId, LastWeek, [], [new SyncedDay(Today, Kcal(2000m)), new SyncedDay(Today, Kcal(2100m))], Today
            )
        );
    }

    [Fact]
    public void Apply_WithADayOfAnotherUser_ThrowsDomainException()
    {
        DiaryDay other = DiaryDay.Create(Guid.CreateVersion7(), Today, Kcal(2000m), Today);

        Assert.Throws<DiaryDayOfAnotherUserDomainException>
        (
            () => DiarySynchronization.Apply(UserId, LastWeek, [other], [], Today)
        );
    }

    [Fact]
    public void Apply_WithASyncedDayInTheFuture_ThrowsDomainException()
    {
        DiaryPeriod period = DiaryPeriod.Create(Today, Today.AddDays(1));

        Assert.Throws<DiaryDayInTheFutureDomainException>
        (
            () => DiarySynchronization.Apply(UserId, period, [], [new SyncedDay(Today.AddDays(1), Kcal(2000m))], Today)
        );
    }
}
