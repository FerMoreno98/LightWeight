namespace LightWeight.UserProfile.Domain.Uow;

public interface IUserProfileUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}