namespace LightWeight.Auth.Infrastructure.Services;

/// <summary>
/// Section "Smtp". Development points to mailpit (no SSL, no account); production to Gmail
/// (smtp.gmail.com:587, EnableSsl, the Gmail address and an app password). Only the values change
/// </summary>
public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }

    /// <summary>Account the API logs in with. Empty: it sends without logging in (mailpit)</summary>
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>Sender address. Gmail only accepts the address of the account itself</summary>
    public string From { get; set; } = "noreply@lightweight.local";
    public string FromName { get; set; } = "LightWeight";
}
