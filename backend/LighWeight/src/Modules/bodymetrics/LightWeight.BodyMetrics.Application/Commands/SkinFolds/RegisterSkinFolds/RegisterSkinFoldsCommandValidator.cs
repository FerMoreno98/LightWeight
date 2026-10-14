using FluentValidation;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.RegisterSkinFolds;

public sealed class RegisterSkinFoldsCommandValidator : AbstractValidator<RegisterSkinFoldsCommand>
{
    public RegisterSkinFoldsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Values)
            .NotNull();

        RuleFor(x => x.MeasuredAt)
            .NotEmpty();
    }
}
