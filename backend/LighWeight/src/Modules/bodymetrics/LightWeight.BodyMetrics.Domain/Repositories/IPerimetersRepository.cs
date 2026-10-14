using LightWeight.BodyMetrics.Domain.Aggregates;

namespace LightWeight.BodyMetrics.Domain.Repositories;

public interface IPerimetersRepository
{
    Task AddAsync(Perimeters perimeters, CancellationToken cancellationToken);
    Task<Perimeters?> GetByIdAsync(Guid measurementId);
    /// <summary>The latest measurements of a user, from the most recent</summary>
    Task<List<Perimeters>> GetLatestOfAUserAsync(Guid userId, int count);
    /// <summary>Deletes the measurement when saving. Call <see cref="Perimeters.Delete"/> first so the event is raised</summary>
    void Remove(Perimeters perimeters);
}
