using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.BodyMetrics.Infrastructure.Persistence.Repositories;

public class WeightMeasurementRepository : IWeightMeasurementRepository
{
    private readonly BodyMetricsDbContext _dbContext;

    public WeightMeasurementRepository(BodyMetricsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WeightMeasurement measurement, CancellationToken cancellationToken)
    {
        await _dbContext.WeightMeasurements.AddAsync(measurement, cancellationToken);
    }

    public async Task<WeightMeasurement?> GetByIdAsync(Guid measurementId)
    {
        return await _dbContext.WeightMeasurements.SingleOrDefaultAsync(w => w.Id == measurementId);
    }

    public async Task<List<WeightMeasurement>> GetLatestOfAUserAsync(Guid userId, int count)
    {
        // The id breaks ties between measurements taken at the same moment, so the order is stable
        return await _dbContext.WeightMeasurements
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.MeasuredAt)
            .ThenByDescending(w => w.Id)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<WeightMeasurement>> GetAllOfAUserAsync(Guid userId)
    {
        return await _dbContext.WeightMeasurements
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.MeasuredAt)
            .ToListAsync();
    }

    public void Remove(WeightMeasurement measurement)
    {
        _dbContext.WeightMeasurements.Remove(measurement);
    }
}
