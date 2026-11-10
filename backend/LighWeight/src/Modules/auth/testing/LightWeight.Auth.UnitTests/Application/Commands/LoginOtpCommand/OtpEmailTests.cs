using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using Xunit;

namespace LightWeight.Auth.Tests.Application.Commands;

public class OtpEmailTests
{
    [Fact]
    public void Subject_contains_the_code()
    {
        Assert.Contains("042137", OtpEmail.Subject("042137"));
    }

    [Fact]
    public void Html_contains_the_code_and_the_minutes()
    {
        string html = OtpEmail.Html("042137", 10);

        Assert.Contains("042137", html);
        Assert.Contains("10 minutos", html);
        Assert.StartsWith("<!doctype html>", html);
    }

    [Fact]
    public void Text_contains_the_code_and_the_minutes()
    {
        string text = OtpEmail.Text("042137", 10);

        Assert.Contains("042137", text);
        Assert.Contains("10 minutos", text);
        Assert.DoesNotContain("<", text);
    }
}
