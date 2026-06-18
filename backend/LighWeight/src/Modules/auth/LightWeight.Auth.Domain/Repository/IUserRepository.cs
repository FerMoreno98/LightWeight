using System.Data;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;

namespace LightWeight.Auth.Domain.Repository;

public interface IUserRepository
{
    Task AddAsync(User user,CancellationToken cancellationToken);
    Task<User?> FindbyIdAsync(Guid userId);
    Task<User?> FindByEmailAsync(string Email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);  
}