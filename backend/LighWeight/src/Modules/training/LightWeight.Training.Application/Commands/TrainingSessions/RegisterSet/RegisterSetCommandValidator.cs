using FluentValidation;

namespace LightWeight.Training.Application.Commands.TrainingSessions.RegisterSet;

public sealed class RegisterSetCommandValidator : AbstractValidator<RegisterSetCommand>
{
    public RegisterSetCommandValidator()
    {
        RuleFor(x => x.TrainingSessionId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.ExerciseId)
            .NotEmpty();

        RuleFor(x => x.Repetitions)
            .InclusiveBetween(0, 1000);

        RuleFor(x => x.Weight)
            .InclusiveBetween(0, 9999.99m);

        RuleFor(x => x.RPE)
            .InclusiveBetween(1, 10);
    }
}
