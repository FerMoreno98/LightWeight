using LightWeight.shared.BuildingBlocks.Persistance;

namespace LightWeight.Auth.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{

    private readonly AuthDbContext _dbContext;

    public UnitOfWork(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // var entries = _dbContext.ChangeTracker.Entries()
        //             .Select(e => new
        //             {
        //                 Entity = e.Entity.GetType().Name,
        //                 State = e.State
        //             })
        //             .ToList();
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}