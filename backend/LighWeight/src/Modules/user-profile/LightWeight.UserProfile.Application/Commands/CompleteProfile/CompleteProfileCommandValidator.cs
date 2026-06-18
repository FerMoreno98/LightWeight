using FluentValidation;
using LightWeight.UserProfile.Domain.Enum;

namespace LightWeight.UserProfile.Application.Commands.CompleteProfile;

public sealed class CompleteProfileCommandValidator : AbstractValidator<CompleteProfileCommand>
{
    public CompleteProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThan(DateTime.UtcNow)
                .WithMessage("Date of birth cannot be in the future")
            .GreaterThan(DateTime.UtcNow.AddYears(-120))
                .WithMessage("Date of birth is not valid");

        RuleFor(x => x.Sex)
            .NotEmpty()
            .Must(s => Enum.TryParse<Sex>(s, ignoreCase: true, out _))
                .WithMessage("Invalid sex value. Allowed values: Male, Female");

        RuleFor(x => x.CurrentStage)
            .NotEmpty()
            .Must(s => Enum.TryParse<TrainingStage>(s, ignoreCase: true, out _))
                .WithMessage("Invalid training stage. Allowed values: Bulk, Cut, Maintenance");
    }
}