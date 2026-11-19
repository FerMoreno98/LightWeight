using LightWeight.Nutrition.Domain.Aggregates;
using LightWeight.Nutrition.Domain.Exceptions;
using LightWeight.Nutrition.Domain.ValueObjects;

namespace LightWeight.Nutrition.Domain.Services;

/// <summary>Totals of a day as the external diary gives them</summary>
public sealed record SyncedDay(DateOnly Date, DailyIntake Intake);

/// <summary>What a synchronization changed: the days to add and the days to remove when saving</summary>
/// <remarks>The updated days are changed in place (they are already tracked), so they are not listed</remarks>
public sealed record DiarySynchronizationResult(IReadOnlyList<DiaryDay> Created, IReadOnlyList<DiaryDay> Deleted);

/// <summary>Brings the recorded days of a period in line with the external diary, which is the source of truth</summary>
public static class DiarySynchronization
{
    /// <summary>
    /// For every day of the period: a synced day not recorded yet is created, a recorded one is updated
    /// (no event if its totals are the same) and a recorded day that did not arrive is deleted,
    /// because the diary omits the days without entries.
    /// </summary>
    /// <param name="userId">Owner of the diary</param>
    /// <param name="period">Days that were read, both included</param>
    /// <param name="recorded">Days of the user already recorded inside the period</param>
    /// <param name="synced">Days that the external diary returned for the period</param>
    /// <param name="today">Current calendar day of the user (Europe/Madrid)</param>
    public static DiarySynchronizationResult Apply
    (
        Guid userId,
        DiaryPeriod period,
        IReadOnlyCollection<DiaryDay> recorded,
        IReadOnlyCollection<SyncedDay> synced,
        DateOnly today
    )
    {
        Dictionary<DateOnly, SyncedDay> syncedByDate = [];
        foreach (SyncedDay day in synced)
        {
            if (!period.Contains(day.Date))
                throw new DiaryDayOutOfPeriodDomainException(day.Date);
            if (!syncedByDate.TryAdd(day.Date, day))
                throw new DuplicatedDiaryDayDomainException(day.Date);
        }

        Dictionary<DateOnly, DiaryDay> recordedByDate = [];
        foreach (DiaryDay day in recorded)
        {
            if (day.UserId != userId)
                throw new DiaryDayOfAnotherUserDomainException();
            if (!period.Contains(day.Date))
                throw new DiaryDayOutOfPeriodDomainException(day.Date);
            if (!recordedByDate.TryAdd(day.Date, day))
                throw new DuplicatedDiaryDayDomainException(day.Date);
        }

        List<DiaryDay> deleted = [];
        foreach (DiaryDay day in recordedByDate.Values)
        {
            if (syncedByDate.TryGetValue(day.Date, out SyncedDay? syncedDay))
            {
                day.Update(syncedDay.Intake);
            }
            else
            {
                day.Delete();
                deleted.Add(day);
            }
        }

        List<DiaryDay> created = syncedByDate.Values
            .Where(day => !recordedByDate.ContainsKey(day.Date))
            .OrderBy(day => day.Date)
            .Select(day => DiaryDay.Create(userId, day.Date, day.Intake, today))
            .ToList();
        return new DiarySynchronizationResult(created, deleted);
    }
}
