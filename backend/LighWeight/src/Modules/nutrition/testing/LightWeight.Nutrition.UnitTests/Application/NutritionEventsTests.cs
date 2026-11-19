using LightWeight.Nutrition.Application.Events;
using LightWeight.Nutrition.Domain.Aggregates;
using LightWeight.Nutrition.Domain.Events;
using LightWeight.Nutrition.Domain.Repositories;
using LightWeight.Nutrition.Domain.Uow;
using LightWeight.Nutrition.Domain.ValueObjects;
using LightWeight.shared.Contracts;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Contracts.DeviceIntegration;
using LightWeight.shared.Contracts.Nutrition;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using NSubstitute;

namespace LightWeight.Nutrition.UnitTests.Application;

public class NutritionEventsTests
{
    // 19/11/2026 08:00 UTC = 09:00 in Madrid
    private static readonly DateTime Now = new(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly IDiaryDayRepository _days = Substitute.For<IDiaryDayRepository>();
    private readonly INutritionUnitOfWork _unitOfWork = Substitute.For<INutritionUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public NutritionEventsTests() => _clock.UtcNow.Returns(Now);

    private static DiaryDay Recorded(DateOnly date, decimal calories)
    {
        DiaryDay day = DiaryDay.Create(UserId, date, DailyIntake.Create(calories, 150m, 250m, 70m), Today);
        day.ClearDomainEvents();
        return day;
    }

    // ---------------------------------------------------------------- Domain events -> integration events

    [Fact]
    public async Task DayRecorded_PublishesDayRecordedWithTheTotals()
    {
        // Arrange
        DiaryDay day = DiaryDay.Create(UserId, Today, DailyIntake.Create(2650m, 180m, 300m, 80m), Today);
        var @event = (DiaryDayRecordedDomainEvent)day.DomainEvents.Single();
        // Act
        await new DiaryDayDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<DiaryDayRecordedIntegrationEvent>(e =>
                e.DiaryDayId == day.Id &&
                e.UserId == UserId &&
                e.Date == Today &&
                e.Calories == 2650m &&
                e.Protein == 180m &&
                e.Carbohydrate == 300m &&
                e.Fat == 80m &&
                e.OccurredAtUtc == @event.OccurredAtUtc &&
                e.EventName == EventNames.Nutrition.DayRecorded),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DayUpdated_PublishesDayRecordedWithTheNewTotals()
    {
        // Arrange
        DiaryDay day = Recorded(Today, 2000m);
        day.Update(DailyIntake.Create(2400m, 160m, 280m, 75m));
        var @event = (DiaryDayUpdatedDomainEvent)day.DomainEvents.Single();
        // Act
        await new DiaryDayDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<DiaryDayRecordedIntegrationEvent>(e => e.DiaryDayId == day.Id && e.Calories == 2400m && e.Fat == 75m),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task DayDeleted_PublishesDayDeleted()
    {
        // Arrange
        DiaryDay day = Recorded(Today, 2000m);
        day.Delete();
        var @event = (DiaryDayDeletedDomainEvent)day.DomainEvents.Single();
        // Act
        await new DiaryDayDomainEventsHandler(_publisher).HandleAsync(@event, default);
        // Assert
        await _publisher.Received(1).PublishAsync
        (
            Arg.Is<DiaryDayDeletedIntegrationEvent>(e =>
                e.DiaryDayId == day.Id &&
                e.UserId == UserId &&
                e.Date == Today &&
                e.EventName == EventNames.Nutrition.DayDeleted),
            Arg.Any<CancellationToken>()
        );
    }

    // ---------------------------------------------------------------- FatSecret synchronization

    [Fact]
    public async Task DiarySynced_CreatesUpdatesAndDeletesTheDaysOfThePeriod()
    {
        // Arrange
        DateOnly from = Today.AddDays(-6);
        DiaryDay changed = Recorded(Today.AddDays(-2), 2000m);
        DiaryDay emptied = Recorded(Today.AddDays(-4), 1800m);
        _days.GetBetweenAsync(UserId, from, Today, Arg.Any<CancellationToken>()).Returns([changed, emptied]);
        var @event = new FatSecretDiarySyncedIntegrationEvent
        (
            UserId, from, Today,
            [new FatSecretDiaryDay(Today.AddDays(-2), 2300m, 150m, 250m, 70m), new FatSecretDiaryDay(Today.AddDays(-1), 2500m, 170m, 290m, 80m)],
            Now
        );
        var handler = new FatSecretDiarySyncedIntegrationEventHandler(_days, _unitOfWork, _clock);
        // Act
        await handler.HandleAsync(@event, default);
        // Assert
        await _days.Received(1).AddAsync
        (
            Arg.Is<DiaryDay>(d => d.UserId == UserId && d.Date == Today.AddDays(-1) && d.Intake.Calories == 2500m),
            Arg.Any<CancellationToken>()
        );
        _days.Received(1).Remove(emptied);
        _days.DidNotReceive().Remove(changed);
        Assert.Equal(2300m, changed.Intake.Calories);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiarySynced_WithADayAfterTodayInMadrid_DoesNotSave()
    {
        // Arrange
        var @event = new FatSecretDiarySyncedIntegrationEvent
        (
            UserId, Today, Today.AddDays(1), [new FatSecretDiaryDay(Today.AddDays(1), 2000m, 150m, 250m, 70m)], Now
        );
        var handler = new FatSecretDiarySyncedIntegrationEventHandler(_days, _unitOfWork, _clock);
        _days.GetBetweenAsync(default, default, default, default).ReturnsForAnyArgs([]);
        // Act
        await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleAsync(@event, default));
        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Rebuild

    [Fact]
    public async Task RebuildRequested_PublishesARecordedEventForEveryDay()
    {
        // Arrange
        DiaryDay[] days = [Recorded(Today.AddDays(-1), 2000m), Recorded(Today, 2500m)];
        _days.GetAllOfAUserAsync(UserId, Arg.Any<CancellationToken>()).Returns([.. days]);
        var handler = new RebuildRequestedIntegrationEventHandler(_days, _publisher, _clock);
        // Act
        await handler.HandleAsync(new RebuildRequestedIntegrationEvent(UserId, Now), default);
        // Assert
        foreach (DiaryDay day in days)
            await _publisher.Received(1).PublishAsync
            (
                Arg.Is<DiaryDayRecordedIntegrationEvent>(e => e.DiaryDayId == day.Id && e.Calories == day.Intake.Calories && e.OccurredAtUtc == Now),
                Arg.Any<CancellationToken>()
            );
    }

    // ---------------------------------------------------------------- Calendar

    [Theory]
    [InlineData(2026, 11, 19, 22, 59, 2026, 11, 19)] // 23:59 in Madrid (winter, UTC+1)
    [InlineData(2026, 11, 19, 23, 0, 2026, 11, 20)]  // 00:00 of the next day in Madrid
    [InlineData(2026, 7, 1, 21, 59, 2026, 7, 1)]     // 23:59 in Madrid (summer, UTC+2)
    [InlineData(2026, 7, 1, 22, 0, 2026, 7, 2)]
    public void UserCalendar_Today_IsTheDayInMadrid(int y, int m, int d, int h, int min, int ey, int em, int ed)
    {
        DateOnly today = UserCalendar.Today(new DateTime(y, m, d, h, min, 0, DateTimeKind.Utc));

        Assert.Equal(new DateOnly(ey, em, ed), today);
    }
}
