using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Repositories;

public class MicrocycleRepository : IMicrocycleRepository
{
    private readonly TrainingDbContext _dbContext;

    public MicrocycleRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Microcycle microcycle, CancellationToken ct)
    {
        await _dbContext.AddAsync(microcycle, ct);
    }

    public async Task<List<Microcycle>> GetByMesocycleIdAsync(Guid MesocycleId)
    {
        return await _dbContext.Microcycles
            .Where(m => m.MesocycleId == MesocycleId)
            .ToListAsync();
    }
}