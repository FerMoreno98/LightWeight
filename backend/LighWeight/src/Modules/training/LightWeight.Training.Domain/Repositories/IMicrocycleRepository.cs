using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

public interface IMicrocycleRepository
{
    Task AddAsync(Microcycle microcycle, CancellationToken cancellationToken);
}