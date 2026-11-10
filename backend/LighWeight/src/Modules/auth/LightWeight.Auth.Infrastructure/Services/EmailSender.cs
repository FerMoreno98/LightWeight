using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using LightWeight.Auth.Domain.Services;
using Microsoft.Extensions.Options;

namespace LightWeight.Auth.Infrastructure.Services;

public class EmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _smtp = options.Value;

    public async Task Send(string recipient, string subject, string htmlBody, string textBody)
    {
        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            // STARTTLS: Gmail on port 587 requires it, mailpit does not support it
            EnableSsl = _smtp.EnableSsl,
        };
        if (!string.IsNullOrWhiteSpace(_smtp.Username))
        {
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.From, _smtp.FromName),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
        };
        message.To.Add(recipient);
        // Plain text first and HTML last: clients show the last version they can render
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));

        await client.SendMailAsync(message);
    }
}
