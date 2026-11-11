using FluentValidation;
using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Application.Commands.Programs.CopySharedProgram;

public sealed class CopySharedProgramCommandValidator : AbstractValidator<CopySharedProgramCommand>
{
    public CopySharedProgramCommandValidator()
    {
        RuleFor(x => x.ShareToken)
            .NotEmpty()
            .Length(Program.ShareTokenLength);

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
