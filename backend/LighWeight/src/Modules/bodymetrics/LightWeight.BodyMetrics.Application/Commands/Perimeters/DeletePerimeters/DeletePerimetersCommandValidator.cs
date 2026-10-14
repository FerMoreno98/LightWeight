using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.DeletePerimeters;

public sealed class DeletePerimetersCommandValidator : AbstractValidator<DeletePerimetersCommand>
{
    public DeletePerimetersCommandValidator()
    {
        RuleFor(x => x.MeasurementId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
