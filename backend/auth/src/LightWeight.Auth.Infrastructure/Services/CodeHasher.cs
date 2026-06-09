using System.Security.Cryptography;
using System.Text;
using LightWeight.Auth.Domain.Services;

namespace LightWeight.Auth.Infrastructure.Services;

public class CodeHasher : ICodeHasher
{
    private const int SaltSize = 16;

    public string HashCode(string plainCode)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(plainCode));
        return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}";
    }

    public bool Verify(string plainCode, string hashCode)
    {
       string[] parts = hashCode.Split('-');
        byte[] hash = Convert.FromHexString(parts[0]);
        byte[] salt = Convert.FromHexString(parts[1]);
        byte[] inputHash = HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(plainCode));
        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}