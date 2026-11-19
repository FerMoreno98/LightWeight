using LightWeight.Nutrition.Domain.Uow;
using LightWeight.shared.BuildingBlocks;
using LightWeight.shared.Messaging;

namespace LightWeight.Nutrition.Infrastructure.Persistence;

internal sealed class NutritionUnitOfWork : INutritionUnitOfWork
{
    private readonly NutritionDbContext _dbContext;
    private readonly IEventDispatcher _eventDispatcher;

    public NutritionUnitOfWork(NutritionDbContext dbContext, IEventDispatcher eventDispatcher)
    {
        _dbContext = dbContext;
        _eventDispatcher = eventDispatcher;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Collected before saving: deleted aggregates are detached by SaveChanges, and their
        // "deleted" event must be dispatched too
        var aggregates = _dbContext.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Select(e => e.Entity)
            .ToList();
        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        aggregates.ForEach(a => a.ClearDomainEvents());
    }
}
