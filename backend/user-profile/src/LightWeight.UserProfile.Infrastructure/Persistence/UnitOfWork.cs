using LightWeight.shared.BuildingBlocks;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Messaging;
using LightWeight.UserProfile.Domain.Aggregates;

namespace LightWeight.UserProfile.Infrastructure.Persistence;

internal sealed class UnitOfWork : IUnitOfWork
{

    private readonly UserProfileDbContext _dbContext;
    private readonly IEventDispatcher   _eventDispatcher;

    public UnitOfWork(UserProfileDbContext dbContext, IEventDispatcher eventDispatcher)
    {
        _dbContext = dbContext;
        _eventDispatcher = eventDispatcher;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var domainEvents = _dbContext.ChangeTracker
        .Entries<AggregateRoot<Guid>>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();
        await _dbContext.SaveChangesAsync(cancellationToken);
        // Despacha después de persistir
        await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        // Limpia los eventos ya despachados
        _dbContext.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .ToList()
            .ForEach(e => e.Entity.ClearDomainEvents());
    }
}