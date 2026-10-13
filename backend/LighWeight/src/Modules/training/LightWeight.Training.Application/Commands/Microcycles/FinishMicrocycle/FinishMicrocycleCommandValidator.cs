using FluentValidation;

namespace LightWeight.Training.Application.Commands.Microcycles.FinishMicrocycle;

public sealed class FinishMicrocycleCommandValidator : AbstractValidator<FinishMicrocycleCommand>
{
    public FinishMicrocycleCommandValidator()
    {
        RuleFor(x => x.MicrocycleId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
