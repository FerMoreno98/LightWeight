using System.Net.Http.Json;
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
            await _http.PostAsJsonAsync("https://api.resend.com/emails", new {
            from = "onboarding@resend.dev",
            to = recipient,
            subject = subject,
            html = body
        });
    }
}