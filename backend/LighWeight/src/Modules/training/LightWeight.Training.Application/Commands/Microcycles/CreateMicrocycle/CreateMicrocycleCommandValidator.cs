using FluentValidation;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;

public sealed class CreateMicrocycleCommandValidator : AbstractValidator<CreateMicrocycleCommand>
{
    public CreateMicrocycleCommandValidator()
    {
        RuleFor(x => x.MesocycleId)
            .NotEmpty();

        RuleFor(x => x.DurationInDays)
            .GreaterThan(0);

        RuleFor(x => x.TrainingDistribution)
            .NotEmpty()
            .IsEnumName(typeof(TrainingDistribution));
    }
}