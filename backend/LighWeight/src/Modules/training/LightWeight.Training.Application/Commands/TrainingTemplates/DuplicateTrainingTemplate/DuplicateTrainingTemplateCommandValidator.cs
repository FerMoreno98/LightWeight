using FluentValidation;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;

public sealed class DuplicateTrainingTemplateCommandValidator : AbstractValidator<DuplicateTrainingTemplateCommand>
{
    public DuplicateTrainingTemplateCommandValidator()
    {
        RuleFor(x => x.TrainingTemplateId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        // Both optional, but when sent they must be valid
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(TrainingTemplate.NameMaxLength)
            .When(x => x.Name is not null);

        RuleFor(x => x.VolumeLandmark)
            .IsEnumName(typeof(VolumeLandmarks), caseSensitive: false)
            .When(x => x.VolumeLandmark is not null);
    }
}
