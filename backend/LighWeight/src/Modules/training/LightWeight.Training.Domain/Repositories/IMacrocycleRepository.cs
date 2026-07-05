using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

public interface IMacrocycleRepository
{
    Task AddAsync(Macrocycle macrocycle, CancellationToken cancellationToken);
}