using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.CorrectWeight;

public sealed class CorrectWeightCommandValidator : AbstractValidator<CorrectWeightCommand>
{
    public CorrectWeightCommandValidator()
    {
        RuleFor(x => x.MeasurementId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.MeasuredAt)
            .NotEmpty();
    }
}
