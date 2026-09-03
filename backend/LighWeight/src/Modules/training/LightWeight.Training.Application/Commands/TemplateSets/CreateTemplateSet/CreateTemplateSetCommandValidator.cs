using FluentValidation;

namespace LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;
public sealed class CreateTemplateSetCommandValidator : AbstractValidator<CreateTemplateSetCommand>
{
    public CreateTemplateSetCommandValidator()
    {
        RuleFor(x => x.ExerciseId)
            .NotEmpty();

        RuleFor(x => x.TemplateSessionId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Min)
            .GreaterThan(0);

        RuleFor(x => x.Max)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.Min)
            .WithMessage("Max must be greater than or equal to Min");

        RuleFor(x => x.ExpectedRIR)
            .InclusiveBetween(0, 10);

        RuleFor(x => x)
            .Must(x => (x.IsDropSet ? 1 : 0) + (x.IsCluster ? 1 : 0) + (x.IsMyoRep ? 1 : 0) <= 1)
            .WithMessage("There can't be 2 or more advance techniques at the same set");
    }
}