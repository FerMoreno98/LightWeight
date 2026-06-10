using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Repository;
using Microsoft.EntityFrameworkCore;


namespace LightWeight.Auth.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _dbContext;

    public UserRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user,cancellationToken);
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _dbContext.Users
            .Include(u => u.OtpCodes)
            .Include(u => u.DeviceTokens)
                .ThenInclude(d => d.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> FindbyIdAsync(Guid userId)
    {
        return await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _dbContext.Users
            .Include(u => u.DeviceTokens)
                .ThenInclude(d => d.RefreshTokens)
            .SingleOrDefaultAsync(u => u.DeviceTokens
                .Any(d => d.RefreshTokens
                    .Any(r => r.Token == refreshToken)));
    }

}