using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

/// <summary>Every read loads the whole aggregate: the macrocycle with its mesocycles and their weeks</summary>
public interface IMacrocycleRepository
{
    Task AddAsync(Macrocycle macrocycle, CancellationToken cancellationToken);
    Task<Macrocycle?> GetByIdAsync(Guid MacrocycleId);
    /// <summary>The macrocycle that contains the given mesocycle</summary>
    Task<Macrocycle?> GetByMesocycleIdAsync(Guid MesocycleId);
    /// <summary>The macrocycle that contains the given week</summary>
    Task<Macrocycle?> GetByMicrocycleIdAsync(Guid MicrocycleId);
    Task<List<Macrocycle>> GetAllOfAUserAsync(Guid UserId);
    /// <summary>The user's macrocycle that is not finished yet (there is at most one)</summary>
    Task<Macrocycle?> GetActiveOfAUserAsync(Guid UserId);
}
