using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Repositories;

public class MesocycleRepository : IMesocycleRepository
{
    private readonly TrainingDbContext _dbContext;

    public MesocycleRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Mesocycle mesocycle,CancellationToken cancellationToken)
    {
        await _dbContext.AddAsync(mesocycle,cancellationToken);
    }

    public async Task<Mesocycle?> GetByIdAsync(Guid MesocycleId)
    {
        return await _dbContext.Mesocycles.SingleOrDefaultAsync(m => m.Id == MesocycleId);
    }
}
