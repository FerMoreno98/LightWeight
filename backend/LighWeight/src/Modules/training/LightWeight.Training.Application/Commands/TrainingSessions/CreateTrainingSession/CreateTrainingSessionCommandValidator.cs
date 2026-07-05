using FluentValidation;

namespace LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;

public sealed class CreateTrainingSessionCommandValidator : AbstractValidator<CreateTrainingSessionCommand>
{
    public CreateTrainingSessionCommandValidator()
    {
        RuleFor(x => x.MicrocycleId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.MotivationLevel)
            .InclusiveBetween(0, 10);

        RuleFor(x => x.SleepLevel)
            .InclusiveBetween(0, 10);

        RuleFor(x => x.DOMSLevel)
            .InclusiveBetween(0, 10);
    }
}
