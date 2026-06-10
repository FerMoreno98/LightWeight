using System.Net.Http.Json;
using System.Net.Mail;
using LightWeight.Auth.Domain.Services;

namespace LightWeight.Auth.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly HttpClient _http;

    public EmailSender(HttpClient http)
    {
        _http = http;
    }

    public async Task Send(string recipient, string subject, string body)
    {
        using var client = new SmtpClient("localhost", 1025);
        var message = new MailMessage("noreply@lightweight.dev", recipient, subject, body)
        {
            IsBodyHtml = true
        };
        await client.SendMailAsync(message);
    }
}