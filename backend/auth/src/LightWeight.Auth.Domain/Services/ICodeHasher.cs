namespace LightWeight.Auth.Domain.Services;

public interface ICodeHasher
{
    string HashCode(string plainCode);

    bool Verify(string plainCode,string hashCode);
}