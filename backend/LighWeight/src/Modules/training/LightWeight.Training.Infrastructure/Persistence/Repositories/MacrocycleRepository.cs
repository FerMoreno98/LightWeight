using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Repositories;

public class MacrocycleRepository : IMacrocycleRepository
{
    private readonly TrainingDbContext _dbContext;

    public MacrocycleRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Macrocycles with their whole hierarchy (mesocycles and weeks)</summary>
    private IQueryable<Macrocycle> Aggregates =>
        _dbContext.Macrocycles
            .Include(m => m.Mesocycles)
            .ThenInclude(m => m.Microcycles)
            .AsSplitQuery();

    public async Task AddAsync(Macrocycle macrocycle, CancellationToken cancellationToken)
    {
        await _dbContext.AddAsync(macrocycle, cancellationToken);
    }

    public async Task<Macrocycle?> GetByIdAsync(Guid MacrocycleId)
    {
        return await Aggregates.SingleOrDefaultAsync(m => m.Id == MacrocycleId);
    }

    public async Task<Macrocycle?> GetByMesocycleIdAsync(Guid MesocycleId)
    {
        return await Aggregates.SingleOrDefaultAsync(m => m.Mesocycles.Any(me => me.Id == MesocycleId));
    }

    public async Task<Macrocycle?> GetByMicrocycleIdAsync(Guid MicrocycleId)
    {
        return await Aggregates.SingleOrDefaultAsync(m => m.Mesocycles.Any(me => me.Microcycles.Any(mi => mi.Id == MicrocycleId)));
    }

    public async Task<List<Macrocycle>> GetAllOfAUserAsync(Guid UserId)
    {
        return await Aggregates
            .Where(m => m.UserId == UserId)
            .ToListAsync();
    }

    public async Task<Macrocycle?> GetActiveOfAUserAsync(Guid UserId)
    {
        return await Aggregates.SingleOrDefaultAsync(m => m.UserId == UserId && m.FinishedAt == null);
    }
}
