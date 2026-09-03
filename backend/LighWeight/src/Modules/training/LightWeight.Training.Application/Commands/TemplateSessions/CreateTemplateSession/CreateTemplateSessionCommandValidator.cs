using FluentValidation;

namespace LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;

public sealed class CreateTemplateSessionCommandValidator : AbstractValidator<CreateTemplateSessionCommand>
{
    public CreateTemplateSessionCommandValidator()
    {
        RuleFor(x => x.TrainingTemplateId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}