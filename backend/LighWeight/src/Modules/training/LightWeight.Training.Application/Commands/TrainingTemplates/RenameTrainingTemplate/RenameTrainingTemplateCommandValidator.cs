using FluentValidation;
using LightWeight.Training.Domain.Entities;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.RenameTrainingTemplate;

public sealed class RenameTrainingTemplateCommandValidator : AbstractValidator<RenameTrainingTemplateCommand>
{
    public RenameTrainingTemplateCommandValidator()
    {
        RuleFor(x => x.TrainingTemplateId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(TrainingTemplate.NameMaxLength);
    }
}
