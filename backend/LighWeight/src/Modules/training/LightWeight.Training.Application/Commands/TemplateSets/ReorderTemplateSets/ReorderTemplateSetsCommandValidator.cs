using FluentValidation;

namespace LightWeight.Training.Application.Commands.TemplateSets.ReorderTemplateSets;

public sealed class ReorderTemplateSetsCommandValidator : AbstractValidator<ReorderTemplateSetsCommand>
{
    public ReorderTemplateSetsCommandValidator()
    {
        RuleFor(x => x.TemplateSessionId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.TemplateSetIds)
            .NotEmpty();

        RuleForEach(x => x.TemplateSetIds)
            .NotEmpty();
    }
}
