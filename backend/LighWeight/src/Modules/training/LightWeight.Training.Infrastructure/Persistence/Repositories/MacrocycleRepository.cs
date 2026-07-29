using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

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
}