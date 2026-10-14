using LightWeight.BodyMetrics.Domain.Aggregates;

namespace LightWeight.BodyMetrics.Domain.Repositories;

public interface IPerimetersRepository
{
    Task AddAsync(Perimeters perimeters, CancellationToken cancellationToken);
    Task<Perimeters?> GetByIdAsync(Guid measurementId);
    /// <summary>Perimeters of a user, from the most recent</summary>
    Task<List<Perimeters>> GetAllOfAUserAsync(Guid userId);
    /// <summary>Deletes the measurement when saving. Call <see cref="Perimeters.Delete"/> first so the event is raised</summary>
    void Remove(Perimeters perimeters);
}
