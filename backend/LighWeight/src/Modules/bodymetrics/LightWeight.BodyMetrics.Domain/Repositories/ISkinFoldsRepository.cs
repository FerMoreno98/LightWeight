using LightWeight.BodyMetrics.Domain.Aggregates;

namespace LightWeight.BodyMetrics.Domain.Repositories;

public interface ISkinFoldsRepository
{
    Task AddAsync(SkinFolds skinFolds, CancellationToken cancellationToken);
    Task<SkinFolds?> GetByIdAsync(Guid measurementId);
    /// <summary>The latest measurements of a user, from the most recent</summary>
    Task<List<SkinFolds>> GetLatestOfAUserAsync(Guid userId, int count);
    /// <summary>Every measurement of a user. Only to publish them again when the dashboard rebuilds its projections</summary>
    Task<List<SkinFolds>> GetAllOfAUserAsync(Guid userId);
    /// <summary>Deletes the measurement when saving. Call <see cref="SkinFolds.Delete"/> first so the event is raised</summary>
    void Remove(SkinFolds skinFolds);
}
