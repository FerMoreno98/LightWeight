using FluentValidation;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

public sealed class CreateMacrocycleCommandValidator : AbstractValidator<CreateMacrocycleCommand>
{
    public CreateMacrocycleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.AimMuscleGroups)
            .NotEmpty()
            .WithMessage("At least one muscle group is required");

        RuleFor(x => x.StartAt)
            .NotEmpty();

        RuleFor(x => x.EndAt)
            .Must((cmd, endAt) => endAt is null || endAt > cmd.StartAt)
            .WithMessage("EndAt must be after StartAt when provided");

        RuleFor(x => x.TrainingStage)
            .NotEmpty()
            .IsEnumName(typeof(TrainingStage));

        RuleFor(x => x.Periodization)
            .NotEmpty()
            .IsEnumName(typeof(Periodization));
    }
}