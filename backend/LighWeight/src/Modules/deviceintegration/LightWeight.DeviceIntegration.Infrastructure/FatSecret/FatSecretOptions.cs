namespace LightWeight.DeviceIntegration.Infrastructure.FatSecret;

/// <summary>
/// Section "FatSecret". The consumer key and secret come from platform.fatsecret.com (the registered application).
/// Without them the module still starts: linking answers "not available" and the nightly run does nothing
/// </summary>
public sealed class FatSecretOptions
{
    public const string Section = "FatSecret";

    public string? ConsumerKey { get; set; }
    public string? ConsumerSecret { get; set; }
    /// <summary>Frontend page FatSecret sends the user back to, e.g. https://domain/integrations/fatsecret/callback</summary>
    public string? CallbackUrl { get; set; }
    /// <summary>Base64 of 32 random bytes (AES-256) to encrypt the tokens in the database. openssl rand -base64 32</summary>
    public string? TokenEncryptionKey { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConsumerKey) &&
        !string.IsNullOrWhiteSpace(ConsumerSecret) &&
        !string.IsNullOrWhiteSpace(CallbackUrl) &&
        !string.IsNullOrWhiteSpace(TokenEncryptionKey);
}
