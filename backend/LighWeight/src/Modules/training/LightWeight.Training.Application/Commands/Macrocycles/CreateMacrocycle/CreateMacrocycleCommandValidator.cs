using FluentValidation;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;

public sealed class CreateMacrocycleCommandValidator : AbstractValidator<CreateMacrocycleCommand>
{
    public CreateMacrocycleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.TrainingStage)
            .NotEmpty()
            .IsEnumName(typeof(TrainingStage), caseSensitive: false);
    }
}
