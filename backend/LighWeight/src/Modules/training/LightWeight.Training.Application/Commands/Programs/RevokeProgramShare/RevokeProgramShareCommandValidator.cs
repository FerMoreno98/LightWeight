using FluentValidation;

namespace LightWeight.Training.Application.Commands.Programs.RevokeProgramShare;

public sealed class RevokeProgramShareCommandValidator : AbstractValidator<RevokeProgramShareCommand>
{
    public RevokeProgramShareCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
