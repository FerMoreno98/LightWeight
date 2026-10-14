using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.BodyMetrics.Infrastructure.Persistence.Repositories;

public class PerimetersRepository : IPerimetersRepository
{
    private readonly BodyMetricsDbContext _dbContext;

    public PerimetersRepository(BodyMetricsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Perimeters perimeters, CancellationToken cancellationToken)
    {
        await _dbContext.Perimeters.AddAsync(perimeters, cancellationToken);
    }

    public async Task<Perimeters?> GetByIdAsync(Guid measurementId)
    {
        return await _dbContext.Perimeters.SingleOrDefaultAsync(p => p.Id == measurementId);
    }

    public async Task<List<Perimeters>> GetAllOfAUserAsync(Guid userId)
    {
        return await _dbContext.Perimeters
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.MeasuredAt)
            .ToListAsync();
    }

    public void Remove(Perimeters perimeters)
    {
        _dbContext.Perimeters.Remove(perimeters);
    }
}
