using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.RegisterPerimeters;

public sealed class RegisterPerimetersCommandValidator : AbstractValidator<RegisterPerimetersCommand>
{
    public RegisterPerimetersCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Values)
            .NotNull();

        RuleFor(x => x.MeasuredAt)
            .NotEmpty();
    }
}
