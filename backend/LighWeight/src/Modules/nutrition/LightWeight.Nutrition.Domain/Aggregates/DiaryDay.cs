using LightWeight.Nutrition.Domain.Events;
using LightWeight.Nutrition.Domain.Exceptions;
using LightWeight.Nutrition.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Nutrition.Domain.Aggregates;

/// <summary>The food diary of a user for one day: the totals of what they ate</summary>
/// <remarks>
/// The diary is kept in FatSecret; this is a copy of its daily totals, so the user never edits it
/// here: it is only created, replaced or deleted by a synchronization (<see cref="Services.DiarySynchronization"/>).
/// <see cref="Date"/> is a calendar day of the user (Europe/Madrid), without time.
/// </remarks>
public sealed class DiaryDay : AggregateRoot<Guid>
{
    private DiaryDay(Guid id, Guid userId, DateOnly date) : base(id)
    {
        UserId = userId;
        Date = date;
    }

    public Guid UserId { get; private set; }
    /// <summary>Calendar day of the user (Europe/Madrid)</summary>
    public DateOnly Date { get; private set; }
    /// <summary>What the user ate that day</summary>
    public DailyIntake Intake { get; private set; } = null!;

    /// <summary>Records the diary of a day for the first time</summary>
    /// <param name="userId">Owner of the diary</param>
    /// <param name="date">Calendar day of the user; it cannot be after <paramref name="today"/></param>
    /// <param name="intake">Totals of the day</param>
    /// <param name="today">Current calendar day of the user (Europe/Madrid)</param>
    public static DiaryDay Create(Guid userId, DateOnly date, DailyIntake intake, DateOnly today)
    {
        if (userId == Guid.Empty)
            throw new UserIdEmptyDomainException();
        if (date > today)
            throw new DiaryDayInTheFutureDomainException(date);
        var day = new DiaryDay(Guid.CreateVersion7(), userId, date) { Intake = intake };
        day.RaiseDomainEvent(new DiaryDayRecordedDomainEvent(day.Id, userId, date, intake));
        return day;
    }

    /// <summary>Replaces the totals of the day. Nothing changes, and no event is raised, if they are the same</summary>
    /// <returns>Whether the totals changed</returns>
    public bool Update(DailyIntake intake)
    {
        if (intake == Intake)
            return false;
        Intake = intake;
        RaiseDomainEvent(new DiaryDayUpdatedDomainEvent(Id, UserId, Date, intake));
        return true;
    }

    /// <summary>Records that the day is deleted. The repository removes it when saving</summary>
    public void Delete()
    {
        RaiseDomainEvent(new DiaryDayDeletedDomainEvent(Id, UserId, Date));
    }
}
