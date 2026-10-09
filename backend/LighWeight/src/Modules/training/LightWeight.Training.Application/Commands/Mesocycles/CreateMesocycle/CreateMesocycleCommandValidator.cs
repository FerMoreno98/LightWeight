using FluentValidation;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

public sealed class CreateMesocycleCommandValidator : AbstractValidator<CreateMesocycleCommand>
{
    public CreateMesocycleCommandValidator()
    {
        RuleFor(x => x.MacrocycleId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.ProgramId)
            .NotEmpty();

        RuleFor(x => x.MotivationLevel)
            .InclusiveBetween(1, 10);
    }
}
