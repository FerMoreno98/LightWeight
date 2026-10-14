using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.CorrectSkinFolds;

public sealed class CorrectSkinFoldsCommandValidator : AbstractValidator<CorrectSkinFoldsCommand>
{
    public CorrectSkinFoldsCommandValidator()
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
