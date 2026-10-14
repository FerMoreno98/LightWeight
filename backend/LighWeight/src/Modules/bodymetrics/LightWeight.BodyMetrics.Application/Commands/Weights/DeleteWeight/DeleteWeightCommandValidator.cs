using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.DeleteWeight;

public sealed class DeleteWeightCommandValidator : AbstractValidator<DeleteWeightCommand>
{
    public DeleteWeightCommandValidator()
    {
        RuleFor(x => x.MeasurementId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
