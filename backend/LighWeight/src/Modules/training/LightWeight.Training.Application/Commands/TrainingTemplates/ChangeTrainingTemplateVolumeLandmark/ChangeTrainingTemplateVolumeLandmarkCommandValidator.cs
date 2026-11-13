using FluentValidation;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.ChangeTrainingTemplateVolumeLandmark;

public sealed class ChangeTrainingTemplateVolumeLandmarkCommandValidator : AbstractValidator<ChangeTrainingTemplateVolumeLandmarkCommand>
{
    public ChangeTrainingTemplateVolumeLandmarkCommandValidator()
    {
        RuleFor(x => x.TrainingTemplateId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.VolumeLandmark)
            .NotEmpty()
            .IsEnumName(typeof(VolumeLandmarks), caseSensitive: false);
    }
}
