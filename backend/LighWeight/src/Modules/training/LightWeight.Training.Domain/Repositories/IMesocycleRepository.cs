using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

public interface IMesocycleRepository
{
    Task AddAsync(Mesocycle mesocycle,CancellationToken cancellationToken);
    Task<Mesocycle?> GetByIdAsync(Guid MesocycleId);
}