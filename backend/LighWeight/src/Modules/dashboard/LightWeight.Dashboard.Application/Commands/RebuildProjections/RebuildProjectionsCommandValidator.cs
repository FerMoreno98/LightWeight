using FluentValidation;

namespace LightWeight.Dashboard.Application.Commands.RebuildProjections;

public sealed class RebuildProjectionsCommandValidator : AbstractValidator<RebuildProjectionsCommand>
{
    public RebuildProjectionsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
