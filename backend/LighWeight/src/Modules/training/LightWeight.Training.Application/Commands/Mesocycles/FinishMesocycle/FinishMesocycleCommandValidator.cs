using FluentValidation;

namespace LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;

public sealed class FinishMesocycleCommandValidator : AbstractValidator<FinishMesocycleCommand>
{
    public FinishMesocycleCommandValidator()
    {
        RuleFor(x => x.MesocycleId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
