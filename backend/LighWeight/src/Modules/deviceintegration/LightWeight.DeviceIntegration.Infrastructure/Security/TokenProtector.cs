using System.Security.Cryptography;
using System.Text;
using LightWeight.DeviceIntegration.Infrastructure.FatSecret;
using Microsoft.Extensions.Options;

namespace LightWeight.DeviceIntegration.Infrastructure.Security;

/// <summary>
/// Encrypts the OAuth tokens before they reach the database (AES-256-GCM). Stored as base64(nonce | tag | ciphertext).
/// The key comes from configuration (FatSecret:TokenEncryptionKey): losing it means every user has to link again
/// </summary>
internal sealed class TokenProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[]? _key;

    public TokenProtector(IOptions<FatSecretOptions> options)
    {
        string? key = options.Value.TokenEncryptionKey;
        if (string.IsNullOrWhiteSpace(key))
            return;
        _key = Convert.FromBase64String(key);
        if (_key.Length != 32)
            throw new InvalidOperationException("FatSecret:TokenEncryptionKey must be the base64 of 32 bytes");
    }

    public string Protect(string plain)
    {
        byte[] key = Key();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
        byte[] output = new byte[NonceSize + TagSize + plainBytes.Length];
        Span<byte> nonce = output.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, output.AsSpan(NonceSize + TagSize), output.AsSpan(NonceSize, TagSize));
        return Convert.ToBase64String(output);
    }

    public string Unprotect(string protectedValue)
    {
        byte[] key = Key();
        byte[] input = Convert.FromBase64String(protectedValue);
        byte[] plainBytes = new byte[input.Length - NonceSize - TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(input.AsSpan(0, NonceSize), input.AsSpan(NonceSize + TagSize), input.AsSpan(NonceSize, TagSize), plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] Key() => _key ?? throw new InvalidOperationException("FatSecret:TokenEncryptionKey is not configured");
}
