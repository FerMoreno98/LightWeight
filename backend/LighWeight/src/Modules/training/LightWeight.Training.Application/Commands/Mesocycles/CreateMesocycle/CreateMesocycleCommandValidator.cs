using FluentValidation;

namespace LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;

public sealed class CreateMesocycleCommandValidator : AbstractValidator<CreateMesocycleCommand>
{
    public CreateMesocycleCommandValidator()
    {
        RuleFor(x => x.MacrocycleId)
            .NotEmpty();

        RuleFor(x => x.MotivationLevel)
            .InclusiveBetween(0, 10);

        RuleFor(x => x.StartAt)
            .NotEmpty()
            .LessThan(x => x.EndAt)
            .WithMessage("StartAt must be before EndAt");

        RuleFor(x => x.EndAt)
            .NotEmpty()
            .GreaterThan(x => x.StartAt)
            .WithMessage("EndAt must be after StartAt");
    }
}