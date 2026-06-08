using System.Data;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.ValueObjects;

namespace LightWeight.Auth.Domain.Repository;

public interface IUserRepository
{
    Task AddAsync(User user,CancellationToken cancellationToken);
    Task<User?> FindbyIdAsync(Guid userId);
    Task KeepOtpCode(string code, CancellationToken cancellationToken);
    Task<OtpCode> GetHashedCodeAsync(string Email);
    Task<User?> FindByEmailAsync(string Email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);  
}