namespace LightWeight.Auth.Domain.Services;

public interface IEmailSender
{
    /// <summary>Sends an HTML email with its plain text version, for the clients that do not show HTML</summary>
    Task Send(string recipient, string subject, string htmlBody, string textBody);
}