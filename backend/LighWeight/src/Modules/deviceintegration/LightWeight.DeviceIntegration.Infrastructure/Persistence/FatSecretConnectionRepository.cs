using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.DeviceIntegration.Domain.Uow;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.DeviceIntegration.Infrastructure.Persistence;

internal sealed class FatSecretConnectionRepository(DeviceIntegrationDbContext dbContext) : IFatSecretConnectionRepository
{
    public async Task AddAsync(FatSecretConnection connection, CancellationToken cancellationToken)
        => await dbContext.FatSecretConnections.AddAsync(connection, cancellationToken);

    public Task<FatSecretConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.FatSecretConnections.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task<List<Guid>> GetLinkedUserIdsAsync(CancellationToken cancellationToken)
        => dbContext.FatSecretConnections
            .Where(c => c.AccessToken != null && c.RevokedAt == null)
            .Select(c => c.UserId)
            .ToListAsync(cancellationToken);

    public void Remove(FatSecretConnection connection) => dbContext.FatSecretConnections.Remove(connection);
}

/// <summary>No domain events in this module: saving is all there is</summary>
internal sealed class DeviceIntegrationUnitOfWork(DeviceIntegrationDbContext dbContext) : IDeviceIntegrationUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
