using FluentValidation;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;

public sealed class DuplicateTrainingTemplateCommandValidator : AbstractValidator<DuplicateTrainingTemplateCommand>
{
    public DuplicateTrainingTemplateCommandValidator()
    {
        RuleFor(x => x.TrainingTemplateId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
