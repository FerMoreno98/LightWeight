using FluentValidation;

namespace LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;

public sealed class CreateTrainingSessionCommandValidator : AbstractValidator<CreateTrainingSessionCommand>
{
    public CreateTrainingSessionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.TemplateSessionId)
            .NotEmpty();

        RuleFor(x => x.MotivationLevel)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.SleepLevel)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.DOMSLevel)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.Comments)
            .MaximumLength(1000);
    }
}
