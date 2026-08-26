using System.Net.Http.Json;
using System.Net.Mail;
using LightWeight.Auth.Domain.Services;
using Microsoft.Extensions.Configuration;

namespace LightWeight.Auth.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration,HttpClient http)
    {
        _http = http;
        _configuration = configuration;
    }

    public async Task Send(string recipient, string subject, string body)
    {
        var host = _configuration["Smtp:Host"];
        var port = int.Parse(_configuration["Smtp:Port"]);

        using var client = new SmtpClient(host, port);
        var message = new MailMessage("noreply@lightweight.dev", recipient, subject, body)
        {
            IsBodyHtml = true
        };
        await client.SendMailAsync(message);
    }
}