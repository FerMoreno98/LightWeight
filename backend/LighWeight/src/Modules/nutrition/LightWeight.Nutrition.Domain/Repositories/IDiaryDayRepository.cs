using LightWeight.Nutrition.Domain.Aggregates;

namespace LightWeight.Nutrition.Domain.Repositories;

public interface IDiaryDayRepository
{
    Task AddAsync(DiaryDay day, CancellationToken cancellationToken);
    /// <summary>The recorded days of a user between two dates, both included</summary>
    Task<List<DiaryDay>> GetBetweenAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
    /// <summary>Every day of a user. Only to publish them again when the dashboard rebuilds its projections</summary>
    Task<List<DiaryDay>> GetAllOfAUserAsync(Guid userId, CancellationToken cancellationToken);
    /// <summary>Deletes the day when saving. Call <see cref="DiaryDay.Delete"/> first so the event is raised</summary>
    void Remove(DiaryDay day);
}
