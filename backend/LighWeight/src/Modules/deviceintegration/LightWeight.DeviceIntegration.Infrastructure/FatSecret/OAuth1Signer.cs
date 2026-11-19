using System.Security.Cryptography;
using System.Text;

namespace LightWeight.DeviceIntegration.Infrastructure.FatSecret;

/// <summary>OAuth 1.0 HMAC-SHA1 signature (RFC 5849, section 3.4)</summary>
internal static class OAuth1Signer
{
    /// <summary>
    /// The signature of a request. <paramref name="url"/> without query string; <paramref name="parameters"/> are every
    /// query/form parameter plus the oauth_* ones, without oauth_signature
    /// </summary>
    public static string Sign(string method, string url, IEnumerable<KeyValuePair<string, string>> parameters,
        string consumerSecret, string? tokenSecret)
    {
        string normalized = string.Join("&", parameters
            .Select(p => (Key: Encode(p.Key), Value: Encode(p.Value)))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .ThenBy(p => p.Value, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}"));

        string baseString = $"{method.ToUpperInvariant()}&{Encode(url)}&{Encode(normalized)}";
        string key = $"{Encode(consumerSecret)}&{Encode(tokenSecret ?? string.Empty)}";

        byte[] hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(baseString));
        return Convert.ToBase64String(hash);
    }

    /// <summary>RFC 3986 percent-encoding: everything but ALPHA, DIGIT, '-', '.', '_' and '~'</summary>
    public static string Encode(string value) => Uri.EscapeDataString(value);

    public static string Nonce() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
