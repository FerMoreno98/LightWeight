using FluentValidation;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;

public sealed class CreateTrainingTemplateCommandValidator : AbstractValidator<CreateTrainingTemplateCommand>
{
    public CreateTrainingTemplateCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(TrainingTemplate.NameMaxLength);

        RuleFor(x => x.VolumeLandmark)
            .NotEmpty()
            .IsEnumName(typeof(VolumeLandmarks));

        RuleFor(x => x.TrainingDistribution)
            .NotEmpty()
            .IsEnumName(typeof(TrainingDistribution));

        RuleFor(x => x.DurationInDays)
            .GreaterThan(0);
    }
}

