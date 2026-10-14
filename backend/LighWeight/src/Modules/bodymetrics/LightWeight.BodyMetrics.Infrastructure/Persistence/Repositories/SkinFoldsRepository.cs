using LightWeight.BodyMetrics.Domain.Aggregates;
using LightWeight.BodyMetrics.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.BodyMetrics.Infrastructure.Persistence.Repositories;

public class SkinFoldsRepository : ISkinFoldsRepository
{
    private readonly BodyMetricsDbContext _dbContext;

    public SkinFoldsRepository(BodyMetricsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SkinFolds skinFolds, CancellationToken cancellationToken)
    {
        await _dbContext.SkinFolds.AddAsync(skinFolds, cancellationToken);
    }

    public async Task<SkinFolds?> GetByIdAsync(Guid measurementId)
    {
        return await _dbContext.SkinFolds.SingleOrDefaultAsync(s => s.Id == measurementId);
    }

    public async Task<List<SkinFolds>> GetLatestOfAUserAsync(Guid userId, int count)
    {
        // The id breaks ties between measurements taken at the same moment, so the order is stable
        return await _dbContext.SkinFolds
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.MeasuredAt)
            .ThenByDescending(s => s.Id)
            .Take(count)
            .ToListAsync();
    }

    public void Remove(SkinFolds skinFolds)
    {
        _dbContext.SkinFolds.Remove(skinFolds);
    }
}
