namespace LightWeight.Training.Domain.Uow;

public interface ITrainingUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}