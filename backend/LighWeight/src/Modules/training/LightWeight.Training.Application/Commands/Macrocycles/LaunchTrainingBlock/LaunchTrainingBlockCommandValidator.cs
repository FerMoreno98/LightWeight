using FluentValidation;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.Macrocycles.LaunchTrainingBlock;

public sealed class LaunchTrainingBlockCommandValidator : AbstractValidator<LaunchTrainingBlockCommand>
{
    public LaunchTrainingBlockCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.TrainingStage)
            .NotEmpty()
            .IsEnumName(typeof(TrainingStage), caseSensitive: false);

        RuleFor(x => x.ProgramId)
            .NotEmpty();

        RuleFor(x => x.MotivationLevel)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.TrainingTemplateId)
            .NotEmpty();
    }
}
