using LightWeight.shared.BuildingBlocks;
using LightWeight.shared.Messaging;
using LightWeight.Training.Domain.Uow;

namespace LightWeight.Training.Infrastructure.Persistence;

internal sealed class TrainingUnitOfWork : ITrainingUnitOfWork
{
    private readonly TrainingDbContext _dbContext;
    private readonly IEventDispatcher _eventDispatcher;

    public TrainingUnitOfWork(TrainingDbContext dbContext, IEventDispatcher eventDispatcher)
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

        await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        _dbContext.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .ToList()
            .ForEach(e => e.Entity.ClearDomainEvents());
    }
}