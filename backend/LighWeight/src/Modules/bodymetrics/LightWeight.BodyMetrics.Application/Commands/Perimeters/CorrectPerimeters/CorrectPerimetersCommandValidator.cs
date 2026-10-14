using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.CorrectPerimeters;

public sealed class CorrectPerimetersCommandValidator : AbstractValidator<CorrectPerimetersCommand>
{
    public CorrectPerimetersCommandValidator()
    {
        RuleFor(x => x.MeasurementId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Values)
            .NotNull();

        RuleFor(x => x.MeasuredAt)
            .NotEmpty();
    }
}
