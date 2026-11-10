using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using Xunit;

namespace LightWeight.Auth.Tests.Application.Commands;

public class SendOtpCodeCommandValidatorTests
{
    private readonly SendOtpCodeCommandValidator _validator = new();

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("nombre.apellido+gym@dominio.es")]
    public void Valid_email_passes(string email)
    {
        Assert.True(_validator.Validate(new SendOtpCodeCommand(email)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nope")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public void Invalid_email_fails(string email)
    {
        Assert.False(_validator.Validate(new SendOtpCodeCommand(email)).IsValid);
    }

    [Fact]
    public void Email_longer_than_the_column_fails()
    {
        string email = new string('a', 190) + "@example.com"; // 202 characters

        Assert.False(_validator.Validate(new SendOtpCodeCommand(email)).IsValid);
    }
}
