namespace LightWeight.BodyMetrics.Domain.Uow;

/// <summary>Saves the changes of the module and then dispatches the domain events of the saved aggregates</summary>
public interface IBodyMetricsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
