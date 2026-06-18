using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Repository;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.UserProfile.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserProfileDbContext _dbContext;

    public UserRepository(UserProfileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user,cancellationToken);
    }

    public Task<User?> FindByIdAsync(Guid UserId)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Id == UserId);
    }
}