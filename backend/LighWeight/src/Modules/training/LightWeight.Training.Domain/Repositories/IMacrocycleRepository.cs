using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

public interface IMacrocycleRepository
{
    Task AddAsync(Macrocycle macrocycle, CancellationToken cancellationToken);
    Task<Macrocycle?> GetByIdAsync(Guid MacrocycleId);
    Task<List<Macrocycle>> GetAllOfAUserAsync(Guid UserId);
    /// <summary>The user's macrocycle that is not finished yet (there is at most one)</summary>
    Task<Macrocycle?> GetActiveOfAUserAsync(Guid UserId);
}
