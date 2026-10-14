using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.DeleteSkinFolds;

public sealed class DeleteSkinFoldsCommandValidator : AbstractValidator<DeleteSkinFoldsCommand>
{
    public DeleteSkinFoldsCommandValidator()
    {
        RuleFor(x => x.MeasurementId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
