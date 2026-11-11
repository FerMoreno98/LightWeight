using FluentValidation;

namespace LightWeight.Training.Application.Commands.Programs.ShareProgram;

public sealed class ShareProgramCommandValidator : AbstractValidator<ShareProgramCommand>
{
    public ShareProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
