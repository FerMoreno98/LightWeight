using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.RegisterWeight;

public sealed class RegisterWeightCommandValidator : AbstractValidator<RegisterWeightCommand>
{
    public RegisterWeightCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.MeasuredAt)
            .NotEmpty();
    }
}
