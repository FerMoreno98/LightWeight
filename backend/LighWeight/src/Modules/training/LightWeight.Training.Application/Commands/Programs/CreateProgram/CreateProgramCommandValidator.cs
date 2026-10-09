using FluentValidation;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.Programs.CreateProgram;

public sealed class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Periodization)
            .NotEmpty()
            .IsEnumName(typeof(Periodization));

        RuleFor(x => x.AimMuscleGroups)
            .NotNull();

        RuleForEach(x => x.AimMuscleGroups)
            .IsEnumName(typeof(MuscleGroups), caseSensitive: false);
    }
}
