using LightWeight.DeviceIntegration.Domain.Aggregates;

namespace LightWeight.DeviceIntegration.Domain.Repositories;

public interface IFatSecretConnectionRepository
{
    Task AddAsync(FatSecretConnection connection, CancellationToken cancellationToken);
    Task<FatSecretConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    /// <summary>The users whose diary can be read (linked and not revoked)</summary>
    Task<List<Guid>> GetLinkedUserIdsAsync(CancellationToken cancellationToken);
    void Remove(FatSecretConnection connection);
}
