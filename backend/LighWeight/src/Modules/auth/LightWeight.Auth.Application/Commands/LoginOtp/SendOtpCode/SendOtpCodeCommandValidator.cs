using FluentValidation;

namespace LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;

/// <summary>
/// Without it an invalid email created the user and then failed when sending the mail (500)
/// </summary>
public sealed class SendOtpCodeCommandValidator : AbstractValidator<SendOtpCodeCommand>
{
    public SendOtpCodeCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(200) // the column is varchar(200)
            .EmailAddress()
                .WithMessage("Email is not valid");
    }
}
