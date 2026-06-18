namespace LightWeight.Auth.Domain.Uow;

public interface IAuthUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}