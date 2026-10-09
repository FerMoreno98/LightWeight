using FluentValidation;

namespace LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;

public sealed class CreateMicrocycleCommandValidator : AbstractValidator<CreateMicrocycleCommand>
{
    public CreateMicrocycleCommandValidator()
    {
        RuleFor(x => x.MesocycleId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.TrainingTemplateId)
            .NotEmpty();
    }
}
