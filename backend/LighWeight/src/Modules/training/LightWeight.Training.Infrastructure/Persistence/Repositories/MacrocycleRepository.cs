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

    public async Task AddAsync(Macrocycle macrocycle, CancellationToken cancellationToken)
    {
        await _dbContext.AddAsync(macrocycle, cancellationToken);
    }

    public async Task<Macrocycle?> GetByIdAsync(Guid MacrocycleId)
    {
       return
        await _dbContext.Macrocycles
        .SingleOrDefaultAsync(m => m.Id == MacrocycleId);
    }

    public async Task<List<Macrocycle>> GetAllOfAUserAsync(Guid UserId)
    {
        return await _dbContext.Macrocycles
            .Where(m => m.UserId == UserId)
            .ToListAsync();
    }

    public async Task<Macrocycle?> GetActiveOfAUserAsync(Guid UserId)
    {
        return await _dbContext.Macrocycles
            .SingleOrDefaultAsync(m => m.UserId == UserId && m.FinishedAt == null);
    }
}