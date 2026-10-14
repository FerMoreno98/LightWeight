using FluentValidation;

namespace LightWeight.Training.Application.Commands.TrainingSessions.UpdateSet;

public sealed class UpdateSetCommandValidator : AbstractValidator<UpdateSetCommand>
{
    public UpdateSetCommandValidator()
    {
        RuleFor(x => x.TrainingSessionId)
            .NotEmpty();

        RuleFor(x => x.SetId)
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
