using FluentValidation;

namespace LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;

public sealed class FinishMacrocycleCommandValidator : AbstractValidator<FinishMacrocycleCommand>
{
    public FinishMacrocycleCommandValidator()
    {
        RuleFor(x => x.MacrocycleId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
