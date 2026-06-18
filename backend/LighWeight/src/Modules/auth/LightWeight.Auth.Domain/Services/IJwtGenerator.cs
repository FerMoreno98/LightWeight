using LightWeight.Auth.Domain.Aggregates;

namespace LightWeight.Auth.Domain.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);   
}