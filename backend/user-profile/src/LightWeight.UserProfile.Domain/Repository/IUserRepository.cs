using LightWeight.UserProfile.Domain.Aggregates;

namespace LightWeight.UserProfile.Domain.Repository;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);
}