using FluentValidation;
using LightWeight.UserProfile.Domain.Enum;

namespace LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;

public sealed class CompleteProfileCommandValidator : AbstractValidator<ChangeTrainingStageCommand>
{
    public CompleteProfileCommandValidator()
    {
        RuleFor(x => x.Stage)
        .NotEmpty()
        .Must(s => Enum.TryParse<TrainingStage>(s, ignoreCase: true, out _))
            .WithMessage("Invalid training stage. Allowed values: Bulk, Cut, Maintenance");
    }
}