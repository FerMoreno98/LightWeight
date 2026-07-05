using FluentValidation;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed class CreateTrainingTemplateCommandValidator : AbstractValidator<CreateTrainingTemplateCommand>
{
    public CreateTrainingTemplateCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TrainingDistribution)
            .NotEmpty()
            .IsEnumName(typeof(TrainingDistribution));
    }
}
