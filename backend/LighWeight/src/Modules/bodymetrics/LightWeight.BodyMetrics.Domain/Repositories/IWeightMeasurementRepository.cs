using LightWeight.BodyMetrics.Domain.Aggregates;

namespace LightWeight.BodyMetrics.Domain.Repositories;

public interface IWeightMeasurementRepository
{
    Task AddAsync(WeightMeasurement measurement, CancellationToken cancellationToken);
    Task<WeightMeasurement?> GetByIdAsync(Guid measurementId);
    /// <summary>The latest measurements of a user, from the most recent</summary>
    Task<List<WeightMeasurement>> GetLatestOfAUserAsync(Guid userId, int count);
    /// <summary>Every measurement of a user. Only to publish them again when the dashboard rebuilds its projections</summary>
    Task<List<WeightMeasurement>> GetAllOfAUserAsync(Guid userId);
    /// <summary>Deletes the measurement when saving. Call <see cref="WeightMeasurement.Delete"/> first so the event is raised</summary>
    void Remove(WeightMeasurement measurement);
}
