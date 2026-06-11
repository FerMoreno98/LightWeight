using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.UserProfile.Domain.Aggregates;

namespace LightWeight.UserProfile.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{

    private readonly UserProfileDbContext _dbContext;

    public UnitOfWork(UserProfileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}