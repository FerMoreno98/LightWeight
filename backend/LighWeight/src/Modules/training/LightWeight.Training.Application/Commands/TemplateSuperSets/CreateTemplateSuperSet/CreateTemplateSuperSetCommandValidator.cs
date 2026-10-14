using FluentValidation;

namespace LightWeight.Training.Application.Commands.TemplateSuperSets.CreateTemplateSuperSet;

public sealed class CreateTemplateSuperSetCommandValidator : AbstractValidator<CreateTemplateSuperSetCommand>
{
    public CreateTemplateSuperSetCommandValidator()
    {
        RuleFor(x => x.TemplateSessionId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.TemplateSetIds)
            .NotNull()
            .Must(ids => ids.Distinct().Count() >= 2)
            .WithMessage("A superset needs at least two sets");
    }
}
