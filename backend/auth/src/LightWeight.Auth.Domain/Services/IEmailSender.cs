namespace auth.Domain.Services;

public interface IEmailSender
{
    Task Send(string recipient, string subject, string body);
}